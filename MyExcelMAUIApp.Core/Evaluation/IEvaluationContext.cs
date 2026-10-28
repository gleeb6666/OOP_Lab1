using System.Numerics;
using MyExcelMAUIApp.Core.Model;

namespace MyExcelMAUIApp.Core.Evaluation;

public interface IEvaluationContext
{
	BigInteger GetCellValue(CellAddress address);
}
