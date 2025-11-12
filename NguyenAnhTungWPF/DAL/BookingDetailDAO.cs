using NguyenAnhTungWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NguyenAnhTungWPF.DAL
{
    public class BookingDetailDAO
    {
        FuminiHotelManagementContext _context;
        public BookingDetailDAO(FuminiHotelManagementContext context)
        {
            _context = context;
        }

        public IEnumerable<BookingDetail> GetByBookingId(int id) => _context.BookingDetails.Where(b => b.BookingReservationId == id).ToList();

        public void Add(BookingDetail bookingDetail)
        {
            _context.BookingDetails.Add(bookingDetail);
            _context.SaveChanges();
        }

        public void Delete(BookingDetail bookingDetail)
        {
            _context.BookingDetails.Remove(bookingDetail);
            _context.SaveChanges();
        }
    }
}
