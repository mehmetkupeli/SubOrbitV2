using Hangfire;
using Microsoft.Extensions.Logging;
using SubOrbitV2.Application.Common.Interfaces;
using SubOrbitV2.Application.Common.Models.Payment;
using SubOrbitV2.Application.Common.Utils;
using SubOrbitV2.Domain.Abstractions;
using SubOrbitV2.Domain.Entities.Billing;
using SubOrbitV2.Domain.Enums;
using SubOrbitV2.Domain.Specifications.Billing;

namespace SubOrbitV2.Infrastructure.Services.BackgroundJobs;

public class NexiBulkDispatcherJob : INexiBulkDispatcherJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INexiClient _nexiClient;
    private readonly ILogger<NexiBulkDispatcherJob> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    public NexiBulkDispatcherJob(IUnitOfWork unitOfWork, INexiClient nexiClient, ILogger<NexiBulkDispatcherJob> logger,IBackgroundJobClient backgroundJobClient)
    {
        _unitOfWork = unitOfWork;
        _nexiClient = nexiClient;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task ProcessBulkChargeAsync(Guid bulkOperationId)
    {
        _logger.LogInformation("Bulk Operation {BulkId} Dispatcher tarafından başlatıldı.", bulkOperationId);

        // 1. BulkOperation'ı Getir
        var bulkOp = await _unitOfWork.Repository<BulkOperation>().GetByIdAsync(bulkOperationId);
        if (bulkOp == null || bulkOp.Status != BulkOperationStatus.Pending) return;

        // 2. Specification kullanarak Faturaları ve Payer bilgilerini Getir (GetQueryable HATASI DÜZELTİLDİ)
        var spec = new InvoicesByBulkOperationIdSpecification(bulkOperationId);
        var invoices = await _unitOfWork.Repository<Invoice>().ListAsync(spec);

        if (!invoices.Any())
        {
            bulkOp.Status = BulkOperationStatus.Completed; // Hiç fatura yoksa işlem bitmiştir
            _unitOfWork.Repository<BulkOperation>().Update(bulkOp);
            await _unitOfWork.SaveChangesAsync(default);
            return;
        }

        // 3. Nexi Payload'ını Hazırla
        var bulkItems = new List<BulkSubscriptionItem>();

        foreach (var invoice in invoices)
        {
            var token = invoice.Payer.NexiCustomerId;
            if (string.IsNullOrEmpty(token)) continue;

            var nexiOrder = new NexiOrder(
                Items: new[]
                {
                    new NexiOrderItem(
                        Reference: invoice.Id.ToString(),
                        Name: "Abonelik Yenileme Faturası",
                        Quantity: 1,
                        Unit: "pcs",
                        UnitPrice: (int)Math.Round(invoice.Subtotal * 100),
                        TaxRate: 0,
                        TaxAmount: (int)Math.Round(invoice.TotalTax * 100),
                        GrossTotalAmount: (int)Math.Round(invoice.TotalAmount * 100),
                        NetTotalAmount: (int)Math.Round(invoice.Subtotal * 100)
                    )
                },
                Amount: (int)Math.Round(invoice.TotalAmount * 100),
                Currency: invoice.Currency,
                Reference: invoice.Id.ToString()
            );

            bulkItems.Add(new BulkSubscriptionItem(token, nexiOrder));
        }

        // 4. Nexi API'sine Vur (Toplu Gönderim)
        var delay = JobHelper.CalculateStatusCheckDelay(bulkOp.ItemCount);
        try
        {
            var externalBulkId = await _nexiClient.BulkChargeSubscriptionsAsync(bulkItems, bulkOp.ProjectId);

            if (!string.IsNullOrEmpty(externalBulkId))
            {
                bulkOp.ExternalBulkId = externalBulkId;
                bulkOp.Status = BulkOperationStatus.Processing;
                bulkOp.CheckCount = 0;
                
                bulkOp.NextCheckTime = DateTime.UtcNow.Add(delay);
                
                _logger.LogInformation("Bulk Operation {BulkId} Nexi'ye iletildi. ExternalId: {ExtId}", bulkOperationId, externalBulkId);
            }
            else
            {
                bulkOp.Status = BulkOperationStatus.Failed;
                _logger.LogError("Nexi Bulk Charge isteği başarısız oldu. BulkId: {BulkId}", bulkOperationId);
            }
        }
        catch (Exception ex)
        {
            bulkOp.Status = BulkOperationStatus.Failed;
            bulkOp.RawResponse = ex.Message;
            _logger.LogError(ex, "Nexi Bulk Charge sırasında sistem hatası.");
        }

        _unitOfWork.Repository<BulkOperation>().Update(bulkOp);
        await _unitOfWork.SaveChangesAsync(default);

        // 5. İŞLEM BAŞARILIYSA DURUM KONTROLÜ (PULL) JOB'UNU KUR
        if (bulkOp.Status == BulkOperationStatus.Processing)
        {
            _backgroundJobClient.Schedule<INexiStatusCheckerJob>(job => job.CheckBulkStatusAsync(bulkOp.Id),delay);
        }
    }
}