using NguyenAnhTungWPF.DAL;
using NguyenAnhTungWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NguyenAnhTungWPF.Repositories
{
    public class BookingDetailRepository :IBookingDetailRepository
    {
        private readonly BookingDetailDAO _bookingDetailDAO;

        public BookingDetailRepository(BookingDetailDAO bookingDetailDAO)
        {
            _bookingDetailDAO = bookingDetailDAO;
        }

        public void Add(BookingDetail b) => _bookingDetailDAO.Add(b);

        public void Delete(BookingDetail selectedDetail) => _bookingDetailDAO.Delete(selectedDetail);

        public IEnumerable<BookingDetail> GetByBookingId(int value) => _bookingDetailDAO.GetByBookingId(value);
    }
}
