#region Imports
using System.Linq.Expressions;
#endregion

namespace GuitarApp.Core.Services;

public interface IRepository<T> where T : class, new()
{
    /// <summary>
    /// Méthode permettant de récupérer l'ensemble des enregistrement
    /// </summary>
    /// <returns></returns>
    Task<List<T>> GetAllAsync();

    /// <summary>
    /// Méthode permettant de retrouver des enregistrements
    /// </summary>
    /// <param name="predicate">Filtre</param>
    /// <returns></returns>
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Méthode permettant de sauvegarder une entité
    /// </summary>
    /// <param name="entity">L'entité a sauvegarder</param>
    /// <returns></returns>
    Task<int> SaveAsync(T entity);

    /// <summary>
    /// Méthode permettant de supprimer une entitée
    /// </summary>
    /// <param name="entity">L'entitée à supprimer</param>
    /// <returns></returns>
    Task<int> DeleteAsync(T entity);
}
