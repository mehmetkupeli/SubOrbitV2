namespace SubOrbitV2.Domain.Abstractions;

/// <summary>
/// Tüm entity'ler için ortak CRUD ve Sorgulama işlemlerini tanımlayan arayüz.
/// Specification Pattern ile güçlendirilmiştir.
/// </summary>
/// <typeparam name="T">İşlem yapılacak Entity tipi.</typeparam>
public interface IGenericRepository<T> where T : BaseEntity
{
    #region Standard CRUD (Standart İşlemler)

    /// <summary>
    /// ID'ye göre tek bir kayıt getirir.
    /// </summary>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Tablodaki tüm kayıtları getirir (Dikkatli kullanılmalı).
    /// </summary>
    Task<IReadOnlyList<T>> ListAllAsync();

    /// <summary>
    /// Yeni bir kayıt ekler.
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Mevcut bir kaydı günceller.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Çoklu kayıt ekler. (Performans için toplu insert)
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Çoklu kayıt günceller. (Performans için toplu update)
    /// </summary>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>
    /// Kaydı "Silindi" (IsDeleted = true) olarak işaretler.
    /// Veri tabanından fiziksel olarak silinmez.
    /// </summary>
    void SoftDelete(T entity);

    /// <summary>
    /// Kaydı veritabanından tamamen ve geri döndürülemez şekilde siler.
    /// (GDPR, Hatalı Kayıt vb. durumlar için)
    /// </summary>
    void HardDelete(T entity);

    #endregion

    #region Specification Methods (Özel Sorgular)

    /// <summary>
    /// Verilen specification (filtre) kurallarına uyan tek bir kaydı getirir.
    /// </summary>
    Task<T?> GetEntityWithSpec(ISpecification<T> spec);

    /// <summary>
    /// Verilen specification (filtre) kurallarına uyan kayıt listesini getirir.
    /// </summary>
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec);

    /// <summary>
    /// Verilen specification (filtre) kurallarına uyan kayıt sayısını döner.
    /// </summary>
    Task<int> CountAsync(ISpecification<T> spec);

    #endregion
}