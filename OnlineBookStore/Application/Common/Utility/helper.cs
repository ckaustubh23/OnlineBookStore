using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.Generic;

namespace OnlineBookStore.Application.Common.Utility
{
    public static class Utility
    {
        public static async Task<List<T>> ReadExcelToListAsync<T>(IFormFile file) where T : new()
        {
            var resultList = new List<T>();

            if (file != null && file.Length > 0)
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    IWorkbook workbook = new XSSFWorkbook(stream);
                    ISheet sheet = workbook.GetSheetAt(0);

                    var properties = typeof(T).GetProperties();

                    for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        IRow row = sheet.GetRow(rowIndex);
                        if (row == null) continue;

                        var dataItem = new T();
                        for (int colIndex = 0; colIndex < row.LastCellNum; colIndex++)
                        {
                            var cell = row.GetCell(colIndex);
                            var cellValue = cell?.ToString();
                            if (string.IsNullOrEmpty(cellValue)) continue;

                            var property = properties[colIndex];
                            if (property != null && property.CanWrite)
                            {
                                try
                                {
                                    var convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
                                    property.SetValue(dataItem, convertedValue);
                                }
                                catch
                                {
                                    // Handle any conversion errors here
                                }
                            }
                        }

                        resultList.Add(dataItem);
                    }
                }
            }

            return resultList;
        }
    }
}
