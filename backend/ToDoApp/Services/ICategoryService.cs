using ToDoApp.DTOs.ToDoDTOs;

namespace ToDoApp.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryResponseDto>> GetAllCategoriesAsync(int userId);
        Task<CategoryResponseDto?> GetCategoryByIdAsync(int categoryId, int userId);
        Task<CategoryResponseDto> CreateCategoryAsync(CategoryCreateUpdateDto categoryDto, int userId);
        Task<bool> UpdateCategoryAsync(int categoryId, CategoryCreateUpdateDto categoryDto, int userId);
        Task<bool> DeleteCategoryAsync(int categoryId, int userId);
    }
}
