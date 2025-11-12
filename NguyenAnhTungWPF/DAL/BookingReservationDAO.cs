using Microsoft.EntityFrameworkCore;
using NguyenAnhTungWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NguyenAnhTungWPF.DAL
{
    public class BookingReservationDAO
    {
        FuminiHotelManagementContext _context;
        public BookingReservationDAO(FuminiHotelManagementContext context)
        {
            _context = context;
        }

        // Luôn Include(b => b.Customer) để hiển thị tên khách hàng
        public IEnumerable<BookingReservation> GetAll() => _context.BookingReservations.Include(b => b.Customer).ToList();
        public IEnumerable<BookingReservation> GetAllByCustomerName(string name) => _context.BookingReservations.Include(b => b.Customer).Where(b => b.Customer.CustomerFullName.Contains(name)).ToList();
        public IEnumerable<BookingReservation> GetByDateRange(DateTime start, DateTime end)
        {
            // Đảm bảo chỉ so sánh phần ngày, bỏ qua giờ
            var endDate = end.Date.AddDays(1); // Bao gồm cả ngày kết thúc
            return _context.BookingReservations
                           .Include(b => b.Customer)
                           .Where(b => b.BookingDate >= start.Date && b.BookingDate < endDate)
                           .ToList();
        }

        public List<BookingReservation> GetByCustomerId(int customerId)
        {
            // Dùng Include để tải chi tiết nếu cần
            return _context.BookingReservations
                           .Where(br => br.CustomerId == customerId)
                           .OrderByDescending(br => br.BookingDate)
                           .ToList();
        }

        public BookingReservation GetById(int id) => _context.BookingReservations.Find(id);

        public void Add(BookingReservation reservation)
        {
            _context.BookingReservations.Add(reservation);
            _context.SaveChanges();
        }

        public void Update(BookingReservation reservation)
        {
            _context.BookingReservations.Update(reservation);
            _context.SaveChanges();
        }
        public void Delete(int id)
        {
            var book = _context.BookingReservations.Find(id);
            if (book != null)
            {
                if (_context.BookingDetails.Where(bd => bd.BookingReservationId == id).Any())
                {
                    book.BookingStatus = "Deactive";
                    _context.BookingReservations.Update(book);
                }
                else
                {
                    _context.BookingReservations.Remove(book);
                }
                _context.SaveChanges();
            }
        }

    }
}