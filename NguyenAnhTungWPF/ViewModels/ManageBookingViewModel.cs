using NguyenAnhTungWPF.Models;
using NguyenAnhTungWPF.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace NguyenAnhTungWPF.ViewModels
{
    public class ManageBookingViewModel : ViewModelBase
    {
        private readonly IBookingReservationRepository _bookingRepo;
        private readonly IBookingDetailRepository _detailRepo;
        public event Action RequestClose;

        private BookingReservation _originalBooking;
        private BookingReservation _currentBooking;
        public BookingReservation CurrentBooking { get => _currentBooking; set { _currentBooking = value; OnPropertyChanged(); } }

        private ObservableCollection<BookingDetail> _bookingDetailsList;
        public ObservableCollection<BookingDetail> BookingDetailsList { get => _bookingDetailsList; set { _bookingDetailsList = value; OnPropertyChanged(); } }

        private BookingDetail _selectedDetail;
        public BookingDetail SelectedDetail { get => _selectedDetail; set { _selectedDetail = value; OnPropertyChanged(); } }

        private string _windowTitle;
        public string WindowTitle { get => _windowTitle; set { _windowTitle = value; OnPropertyChanged(); } }

        private string _errorMessage;
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RemoveDetailCommand { get; }

        public ManageBookingViewModel(IBookingReservationRepository bookingRepo, IBookingDetailRepository detailRepo)
        {
            _bookingRepo = bookingRepo;
            _detailRepo = detailRepo;

            SaveCommand = new RelayCommand(SaveChanges);
            CancelCommand = new RelayCommand(Cancel);
            RemoveDetailCommand = new RelayCommand(RemoveDetail, CanRemoveDetail);
        }

        public void Initialize(int? bookingId)
        {
            if (bookingId == null)
            {
                ErrorMessage = "No booking selected.";
                return;
            }

            WindowTitle = $"Manage Booking #{bookingId}";
            _originalBooking = _bookingRepo.GetById(bookingId.Value);

            // Tạo bản sao để chỉnh sửa (nếu cần)
            CurrentBooking = new BookingReservation
            {
                BookingReservationId = _originalBooking.BookingReservationId,
                BookingDate = _originalBooking.BookingDate,
                TotalPrice = _originalBooking.TotalPrice,
                CustomerId = _originalBooking.CustomerId,
                BookingStatus = _originalBooking.BookingStatus,
                Customer = _originalBooking.Customer 
            };

            var details = _detailRepo.GetByBookingId(bookingId.Value);
            BookingDetailsList = new ObservableCollection<BookingDetail>(details);
        }

        private bool CanRemoveDetail(object param) => SelectedDetail != null;

        private void RemoveDetail(object param)
        {
            if (SelectedDetail == null) return;

            if (MessageBox.Show($"Are you sure you want to remove Room {SelectedDetail.Room?.RoomNumber} from this booking?",
                                "Confirm Delete",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // Xóa khỏi DB
                    _detailRepo.Delete(SelectedDetail);
                    // Xóa khỏi List
                    BookingDetailsList.Remove(SelectedDetail);

                    // Cập nhật lại tổng tiền
                    UpdateTotalPrice();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not remove detail: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveChanges(object param)
        {
            try
            {
                // Cập nhật lại tổng tiền lần cuối
                UpdateTotalPrice();

                // Cập nhật thông tin từ CurrentBooking (bản sao) về _originalBooking
                _originalBooking.BookingDate = CurrentBooking.BookingDate;
                _originalBooking.BookingStatus = CurrentBooking.BookingStatus;
                _originalBooking.TotalPrice = CurrentBooking.TotalPrice;

                _bookingRepo.Update(_originalBooking);
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving: {ex.Message}";
            }
        }

        private void UpdateTotalPrice()
        {
            CurrentBooking.TotalPrice = BookingDetailsList.Sum(d => d.ActualPrice ?? 0);
            OnPropertyChanged(nameof(CurrentBooking)); // Báo cho UI biết CurrentBooking (và TotalPrice của nó) đã thay đổi
        }

        private void Cancel(object param)
        {
            RequestClose?.Invoke();
        }
    }
}