using Microsoft.Extensions.DependencyInjection;
using NguyenAnhTungWPF.Models;
using NguyenAnhTungWPF.Repositories;
using NguyenAnhTungWPF.Services;
using NguyenAnhTungWPF.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NguyenAnhTungWPF.ViewModels
{

    public class CustomerProfileViewModel : ViewModelBase
    {
        // Services và Repositories
        private readonly IUserSessionService _sessionService;
        private readonly ICustomerRepository _customerRepository;
        private readonly IBookingReservationRepository _bookingRepository;
        private readonly IBookingDetailRepository _bookingDetailRepository;
        private readonly IRoomInformationRepository _roomRepository;
        private readonly IServiceProvider _serviceProvider; 

        public event Action RequestClose; // Để đóng cửa sổ này

        private Customer _currentCustomer;

        // --- Thuộc tính cho Tab "My Profile" ---
        private string _fullName;
        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }

        private string _email;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }

        private string _telephone;
        public string Telephone { get => _telephone; set { _telephone = value; OnPropertyChanged(); } }

        private DateTime? _birthday;
        public DateTime? Birthday { get => _birthday; set { _birthday = value; OnPropertyChanged(); } }

        private string _password;
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

        private string _errorMessage;
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }

        // --- Thuộc tính cho Tab "Booking History" ---
        public ObservableCollection<BookingReservation> BookingHistory { get; set; }

        // --- Thuộc tính cho Tab "New Booking" ---
        public ObservableCollection<RoomInformation> AvailableRooms { get; set; }

        private DateTime? _newBookingStartDate = DateTime.Now;
        public DateTime? NewBookingStartDate { get => _newBookingStartDate; set { _newBookingStartDate = value; OnPropertyChanged(); } }

        private DateTime? _newBookingEndDate = DateTime.Now.AddDays(1);
        public DateTime? NewBookingEndDate { get => _newBookingEndDate; set { _newBookingEndDate = value; OnPropertyChanged(); } }

        private RoomInformation _selectedAvailableRoom;
        public RoomInformation SelectedAvailableRoom { get => _selectedAvailableRoom; set { _selectedAvailableRoom = value; OnPropertyChanged(); } }

        private string _bookingErrorMessage;
        public string BookingErrorMessage { get => _bookingErrorMessage; set { _bookingErrorMessage = value; OnPropertyChanged(); } }


        // === PHẦN MỚI CHO GIỎ HÀNG (BOOKING CART) ===
        public ObservableCollection<BookingDetail> PendingBookingDetails { get; set; }

        private BookingDetail _selectedPendingDetail;
        public BookingDetail SelectedPendingDetail { get => _selectedPendingDetail; set { _selectedPendingDetail = value; OnPropertyChanged(); } }

        private decimal _totalBookingPrice;
        public decimal TotalBookingPrice { get => _totalBookingPrice; set { _totalBookingPrice = value; OnPropertyChanged(); } }
        // === KẾT THÚC PHẦN MỚI ===


        // --- Commands ---
        public ICommand UpdateProfileCommand { get; }
        public ICommand LogoutCommand { get; }

        // === COMMANDS MỚI CHO TAB "NEW BOOKING" ===
        public ICommand AddDetailCommand { get; }
        public ICommand RemoveDetailCommand { get; }
        public ICommand CreateBookingCommand { get; } // Thay thế cho BookRoomCommand


        public CustomerProfileViewModel(
            IUserSessionService sessionService,
            ICustomerRepository customerRepository,
            IBookingReservationRepository bookingRepository,
            IBookingDetailRepository bookingDetailRepository,
            IRoomInformationRepository roomRepository,
            IServiceProvider serviceProvider)
        {
            _sessionService = sessionService;
            _customerRepository = customerRepository;
            _bookingRepository = bookingRepository;
            _bookingDetailRepository = bookingDetailRepository;
            _roomRepository = roomRepository;
            _serviceProvider = serviceProvider;

            BookingHistory = new ObservableCollection<BookingReservation>();
            AvailableRooms = new ObservableCollection<RoomInformation>();

            // Khởi tạo giỏ hàng
            PendingBookingDetails = new ObservableCollection<BookingDetail>();

            UpdateProfileCommand = new RelayCommand(ExecuteUpdateProfile, CanExecute);
            LogoutCommand = new RelayCommand(ExecuteLogout, CanExecute);
            AddDetailCommand = new RelayCommand(ExecuteAddDetail, CanExecuteAddDetail);
            RemoveDetailCommand = new RelayCommand(ExecuteRemoveDetail, CanExecuteRemoveDetail);
            CreateBookingCommand = new RelayCommand(ExecuteCreateBooking, CanExecuteCreateBooking);

            LoadCustomerData();
            LoadBookingHistory();
            LoadAvailableRooms();
        }

        private void LoadCustomerData()
        {
            _currentCustomer = _sessionService.CurrentCustomer;
            if (_currentCustomer != null)
            {
                // Tải lại thông tin mới nhất từ DB
                _currentCustomer = _customerRepository.GetById(_currentCustomer.CustomerId);

                FullName = _currentCustomer.CustomerFullName;
                Email = _currentCustomer.EmailAddress;
                Telephone = _currentCustomer.Telephone;
                Birthday = _currentCustomer.CustomerBirthday.HasValue
                    ? _currentCustomer.CustomerBirthday.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null;
                Password = _currentCustomer.Password;
            }
        }

        private void LoadBookingHistory()
        {
            if (_currentCustomer == null) return;
            var history = _bookingRepository.GetByCustomerId(_currentCustomer.CustomerId);
            BookingHistory.Clear();
            foreach (var item in history)
            {
                BookingHistory.Add(item);
            }
        }

        private void LoadAvailableRooms()
        {
            var rooms = _roomRepository.GetAvailableRooms();
            AvailableRooms.Clear();
            foreach (var room in rooms)
            {
                AvailableRooms.Add(room);
            }
        }

        private bool CanExecute(object obj) => true;

        private void ExecuteUpdateProfile(object obj)
        {
            try
            {
                _currentCustomer.CustomerFullName = FullName;
                _currentCustomer.Telephone = Telephone;
                _currentCustomer.CustomerBirthday = Birthday.HasValue
                    ? DateOnly.FromDateTime(Birthday.Value)
                    : DateOnly.FromDateTime(DateTime.Now);
                _currentCustomer.Password = Password;

                _customerRepository.Update(_currentCustomer);
                ErrorMessage = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating profile: {ex.Message}";
            }
        }

        private void ExecuteLogout(object obj)
        {
            _sessionService.Logout();
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
            RequestClose?.Invoke();
        }

        private bool CanExecuteAddDetail(object obj)
        {
            return SelectedAvailableRoom != null && NewBookingStartDate.HasValue && NewBookingEndDate.HasValue;
        }

        private void ExecuteAddDetail(object obj)
        {
            try
            {
                BookingErrorMessage = ""; 
                if (NewBookingEndDate <= NewBookingStartDate)
                {
                    BookingErrorMessage = "End date must be after start date.";
                    return;
                }

                int totalDays = (int)(NewBookingEndDate.Value.Date - NewBookingStartDate.Value.Date).TotalDays;
                if (totalDays <= 0)
                {
                    BookingErrorMessage = "Booking must be for at least 1 night.";
                    return;
                }

                if (_roomRepository.IsRoomAvailable(SelectedAvailableRoom.RoomId, _currentCustomer.CustomerId) == false)
                {
                    BookingErrorMessage = "You have booked this room.";
                    return;
                }

                // Tạo một BookingDetail mới
                foreach(var booking in PendingBookingDetails)
                {
                    if(booking.RoomId==SelectedAvailableRoom.RoomId)
                    {
                        BookingErrorMessage = "You have select this rooom.";
                        return;
                    }
                }
                var detail = new BookingDetail
                {
                    RoomId = SelectedAvailableRoom.RoomId,
                    Room = SelectedAvailableRoom, // Gán object để DataGrid hiển thị được tên phòng
                    StartDate = DateOnly.FromDateTime(NewBookingStartDate.Value),
                    EndDate = DateOnly.FromDateTime(NewBookingEndDate.Value),
                    ActualPrice = SelectedAvailableRoom.RoomPricePerDay * totalDays
                };

                PendingBookingDetails.Add(detail);
                UpdateTotalBookingPrice();
            }
            catch (Exception ex)
            {
                BookingErrorMessage = $"Error adding room: {ex.Message}";
            }
        }

        private bool CanExecuteRemoveDetail(object obj)
        {
            // Chỉ cho phép xóa khi có một item trong giỏ hàng được chọn
            return SelectedPendingDetail != null;
        }

        private void ExecuteRemoveDetail(object obj)
        {
            PendingBookingDetails.Remove(SelectedPendingDetail);
            UpdateTotalBookingPrice();
        }

        private bool CanExecuteCreateBooking(object obj)
        {
            // Chỉ cho phép đặt khi giỏ hàng có ít nhất 1 món
            return PendingBookingDetails.Count > 0;
        }

        private void ExecuteCreateBooking(object obj)
        {
            if(MessageBox.Show("Are you sure to book these rooms?", "Confirm booking",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    BookingErrorMessage = ""; // Xóa lỗi cũ

                    var reservation = new BookingReservation
                    {
                        BookingDate = DateTime.Now,
                        TotalPrice = TotalBookingPrice, 
                        CustomerId = _currentCustomer.CustomerId,
                        BookingStatus = "Active",
                        BookingDetails = PendingBookingDetails
                    };

                    _bookingRepository.Add(reservation);

                    PendingBookingDetails.Clear();
                    UpdateTotalBookingPrice();
                    BookingErrorMessage = "Booking successful!";

                    LoadBookingHistory();
                }
                catch (Exception ex)
                {
                    string innerEx = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    BookingErrorMessage = $"Error creating booking: {innerEx}";
                }
            }
            
        }

        private void UpdateTotalBookingPrice()
        {
            // Tính tổng tiền từ các item trong giỏ hàng
            TotalBookingPrice = PendingBookingDetails.Sum(d => d.ActualPrice.Value);
        }
    }
}