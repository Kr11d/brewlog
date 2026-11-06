namespace BrewLog.Domain.Common;

/// <summary>Сутність із числовим ідентифікатором у БД; завдяки цьому репозиторії можна узагальнити.</summary>
public interface IEntity
{
    int Id { get; set; }
}
