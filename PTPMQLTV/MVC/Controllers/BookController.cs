using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC.Data;
using MVC.Models;
using OfficeOpenXml;

namespace MVC.Controllers
{
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy ra toàn bộ danh sách sinh viên mượn sách
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            return _context.Book != null ?
                          View(await _context.Book.ToListAsync()) :
                          View();
        }
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Tạo mới sinh viên mượn sách
        /// </summary>
        /// <param name="sinhVien"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdBook,NameBook,Number,Year,NhaXuatBan")] Book book)
        {
            // Check if a book with the same IdBook already exists
            if (BookExists(book.IdBook))
            {
                ModelState.AddModelError("IdBook", "Mã sách đã tồn tại, vui lòng chọn mã sách khác");
                return View(book);
            }
            if (ModelState.IsValid)
            {
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }
        // Check if a book with the same IdBook already exists
        private bool BookExists(int id)
        {
            // Check if a book with the given IdBook already exists in the database
            return _context.Book.Any(e => e.IdBook == id);
        }
        /// <summary>
        /// Chi tiết của sách
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Details(int id)
        {
            if (id == 0 || _context.Book == null)
            {
                return NotFound();
            }

            var book = await _context.Book
                .FirstOrDefaultAsync(m => m.IdBook == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        /// <summary>
        /// Hiển thị màn confirm xoá sách vừa chọn
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (id == 0 || _context.Book == null)
            {
                return NotFound();
            }

            var bookDetail = await _context.Book
                .Where(m => m.IdBook == id).FirstOrDefaultAsync();

            if (bookDetail == null)
            {
                return NotFound();
            }

            return View(bookDetail);
        }

        /// <summary>
        /// Xác nhận xoá
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Book == null)
            {
                return Problem("Không tìm thấy nhân viên");
            }
            var bookDelete = await _context.Book.FindAsync(id);
            if (bookDelete != null)
            {
                _context.Book.Remove(bookDelete);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (id == 0 || _context.Book == null)
            {
                return NotFound();
            }

            var bookDetail = await _context.Book
                .Where(m => m.IdBook == id).FirstOrDefaultAsync();

            if (bookDetail == null)
            {
                return NotFound();
            }
            return View(bookDetail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdBook,NameBook,Number,NhaXuatBan,Year")] Book book)
        {
            if (id != book.IdBook)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {

                }
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        public async Task<IActionResult> ExportBooksToExcel()
        {
            // Lấy danh sách sách từ cơ sở dữ liệu
            List<Book> result = await _context.Book.ToListAsync();

            // Tạo một đối tượng ExcelPackage
            using (var package = new ExcelPackage())
            {
                // Tạo một trang tính mới
                var worksheet = package.Workbook.Worksheets.Add("Books");

                // Ghi dữ liệu vào trang tính
                worksheet.Cells["A1"].Value = "Mã Sách";
                worksheet.Cells["B1"].Value = "Tên Sách";
                worksheet.Cells["C1"].Value = "Số Lượng";
                worksheet.Cells["D1"].Value = "Nhà Xuất Bản";
                worksheet.Cells["E1"].Value = "Năm Xuất Bản";
                // Add other headers as needed

                var row = 2;
                foreach (var book in result)
                {
                    worksheet.Cells[$"A{row}"].Value = book.IdBook;
                    worksheet.Cells[$"B{row}"].Value = book.NameBook;
                    worksheet.Cells[$"C{row}"].Value = book.Number;
                    worksheet.Cells[$"D{row}"].Value = book.NhaXuatBan;
                    worksheet.Cells[$"E{row}"].Value = book.Year;
                    // Add other book properties as needed
                    row++;
                }
                // Đặt kích thước cột tự động dựa trên nội dung
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Lưu ExcelPackage vào một mảng byte
                var excelBytes = package.GetAsByteArray();

                // Xác định loại file MIME
                var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                // Trả về FileResult
                return File(excelBytes, mimeType, "Books.xlsx");
            }
        }
    }
}
