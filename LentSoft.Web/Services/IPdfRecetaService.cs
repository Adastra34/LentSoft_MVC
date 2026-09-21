using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IPdfRecetaService
{
    byte[] GenerateRecetaPdf(FormulaOptica formula);
}
