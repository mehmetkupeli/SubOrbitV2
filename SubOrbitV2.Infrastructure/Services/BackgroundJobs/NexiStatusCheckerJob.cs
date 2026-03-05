using Hangfire;
using Microsoft.Extensions.Logging;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Entities.Organization;
using SubOrbitV2.Domain.Enums;
using SubOrbitV2.Domain.Specifications.Billing;
using SubOrbitV2.Domain.Specifications.Organization;

namespace SubOrbitV2.Infrastructure.Services.BackgroundJobs;

public class NexiStatusCheckerJob : INexiStatusCheckerJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INexiClient _nexiClient;
    private readonly ILogger<NexiStatusCheckerJob> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IProjectContext _projectContext;
    public NexiStatusCheckerJob(
        IUnitOfWork unitOfWork,
        INexiClient nexiClient,
        ILogger<NexiStatusCheckerJob> logger,
        IBackgroundJobClient backgroundJobClient,
        IProjectContext projectContext)
    {
        _unitOfWork = unitOfWork;
        _nexiClient = nexiClient;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
        _projectContext = projectContext;
    }

    public async Task CheckBulkStatusAsync(Guid projectId,Guid bulkOperationId)
    {
        _logger.LogInformation("Mutabakat Bekçisi uyandı. BulkOperationId: {BulkId}", bulkOperationId);
        
        #region 1. HAYATİ ADIM: PROJECT CONTEXT'İ DOLDUR (HYDRATE)
        // Hem Global Query Filter hem de NexiClient için bağlamı oluşturuyoruz
        _projectContext.SetProjectId(projectId);

        var projectSpec = new ProjectWithSettingsByIdSpecification(projectId);
        var project = await _unitOfWork.Repository<Project>().GetEntityWithSpec(projectSpec);

        if (project == null) return;
        _projectContext.SetProject(project); // NexiClient artık API Key'i okuyabilecek!
        
        #endregion
        // 1. Bulk Operation Kaydını Getir
        var bulkOp = await _unitOfWork.Repository<BulkOperation>().GetByIdAsync(bulkOperationId);
        if (bulkOp == null || string.IsNullOrEmpty(bulkOp.ExternalBulkId)) return;

        // Zaten tamamlanmışsa veya hata almışsa işlemi kes
        if (bulkOp.Status == BulkOperationStatus.Completed || bulkOp.Status == BulkOperationStatus.Failed)
            return;

        bulkOp.CheckCount++;

        // 2. Bu Bulk operasyonuna bağlı açık faturaları (ve Payer'ları) getir
        var spec = new InvoicesByBulkOperationIdSpecification(bulkOperationId);
        var invoices = await _unitOfWork.Repository<Invoice>().ListAsync(spec);

        int pageNumber = 1;
        bool hasMore = true;
        bool anyFailed = false;
        bool isStillProcessing = false;

        // 3. Nexi'den Durumu Çek (Sayfalama ile)
        while (hasMore)
        {
            var response = await _nexiClient.RetrieveBulkChargeStatusAsync(bulkOp.ExternalBulkId, pageNumber);

            if (response == null)
            {
                _logger.LogWarning("Nexi'den durum alınamadı. BulkId: {BulkId}. Daha sonra tekrar denenecek.", bulkOperationId);
                RescheduleJob(bulkOp, projectId);
                return;
            }

            // Nexi tarafında işlem hala sürüyorsa döngüyü kır, job'ı ertele
            if (response.Status == "Processing")
            {
                _logger.LogInformation("Nexi hala işlemi sürdürüyor. 1 saat sonra tekrar kontrol edilecek.");
                isStillProcessing = true;
                break;
            }

            // Nexi işlemi bitirmiş ("Done"), dönen sonuçları bizim faturalarla eşleştiriyoruz
            foreach (var item in response.Pages)
            {
                // Nexi'ye "SubscriptionId" alanı içine Payer'ın "NexiCustomerId" (Token) bilgisini göndermiştik.
                var invoice = invoices.FirstOrDefault(i => i.Payer.NexiCustomerId == item.SubscriptionId && i.Status == InvoiceStatus.Open);

                if (invoice != null)
                {
                    if (item.Status == "Succeeded")
                    {
                        invoice.Status = InvoiceStatus.Paid;
                        invoice.AmountPaid = invoice.TotalAmount;
                        invoice.NexiTransactionId = item.ChargeId;
                        invoice.PaidAt = DateTime.UtcNow;
                        _unitOfWork.Repository<Invoice>().Update(invoice);
                    }
                    else
                    {
                        anyFailed = true;

                        string errorDetail = item.Error?.ToString() ?? "Bilinmeyen Hata (Unknown Error)";

                        _logger.LogWarning("Tahsilat başarısız. Fatura No: {InvoiceNo}, Hata: {Error}", invoice.Number, (object)errorDetail);
                    }
                }
            }

            hasMore = response.More;
            pageNumber++;
        }

        if (isStillProcessing)
        {
            RescheduleJob(bulkOp, projectId);
            return;
        }

        // 4. Sonuçları Kaydet
        bulkOp.Status = anyFailed ? BulkOperationStatus.PartiallyFailed : BulkOperationStatus.Completed;
        _unitOfWork.Repository<BulkOperation>().Update(bulkOp);

        await _unitOfWork.SaveChangesAsync(default);

        _logger.LogInformation("Mutabakat başarıyla tamamlandı. BulkOperationId: {BulkId}. Son Durum: {Status}", bulkOperationId, bulkOp.Status);
    }

    /// <summary>
    /// Eğer işlem bitmediyse veya Nexi yanıt vermediyse, job'ı maksimum 24 kez (24 saat) olmak üzere tekrar kurar.
    /// </summary>
    private void RescheduleJob(BulkOperation bulkOp,Guid projectId)
    {
        if (bulkOp.CheckCount < 24)
        {
            bulkOp.NextCheckTime = DateTime.UtcNow.AddHours(1);
            _unitOfWork.Repository<BulkOperation>().Update(bulkOp);

            // Senkron bekletiyoruz ki DB'ye güncel NextCheckTime yazılsın
            _unitOfWork.SaveChangesAsync(default).Wait();

            _backgroundJobClient.Schedule<INexiStatusCheckerJob>(
                x => x.CheckBulkStatusAsync(projectId,bulkOp.Id),
                TimeSpan.FromHours(1)
            );
        }
        else
        {
            bulkOp.Status = BulkOperationStatus.Failed;
            _unitOfWork.Repository<BulkOperation>().Update(bulkOp);
            _unitOfWork.SaveChangesAsync(default).Wait();
            _logger.LogError("Bulk Operation {BulkId} 24 saat boyunca tamamlanamadı ve Failed olarak işaretlendi.", bulkOp.Id);
        }
    }
}