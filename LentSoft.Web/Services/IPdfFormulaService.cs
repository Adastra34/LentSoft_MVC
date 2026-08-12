using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IPdfFormulaService
{
    byte[] GenerateFormulaPdf(FormulaOptica formula);
}
