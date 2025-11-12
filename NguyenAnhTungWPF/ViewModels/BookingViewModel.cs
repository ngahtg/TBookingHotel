using NguyenAnhTungWPF.Models;
using NguyenAnhTungWPF.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace NguyenAnhTungWPF.ViewModels
{
    public class BookingViewModel : ViewModelBase
    {
        // Repositories
        private readonly ICustomerRepository _customerRepo;
        private readonly IBookingReservationRepository _bookingRepo;
        private readonly IBookingDetailRepository _detailRepo;
        private readonly IRoomInformationRepository _roomRepo;

        public event Action RequestClose;

        private ObservableCollection<Customer> _allCustomers;
        public ObservableCollection<Customer> AllCustomers { get => _allCustomers; set { _allCustomers = value; OnPropertyChanged(); } }

        private Customer _selectedCustomer;
        public Customer SelectedCustomer { get => _selectedCustomer; set { _selectedCustomer = value; OnPropertyChanged(); } }
        
        
        public ObservableCollection<RoomInformation> AvailableRooms { get; set; }

        private DateTime? _newBookingStartDate = DateTime.Now;
        public DateTime? NewBookingStartDate { get => _newBookingStartDate; set { _newBookingStartDate = value; OnPropertyChanged(); } }

        private DateTime? _newBookingEndDate = DateTime.Now.AddDays(1);
        public DateTime? NewBookingEndDate { get => _newBookingEndDate; set { _newBookingEndDate = value; OnPropertyChanged(); } }

        private RoomInformation _selectedAvailableRoom;
        public RoomInformation SelectedAvailableRoom { get => _selectedAvailableRoom; set { _selectedAvailableRoom = value; OnPropertyChanged(); } }

        private string _bookingErrorMessage;
        public string BookingErrorMessage { get => _bookingErrorMessage; set { _bookingErrorMessage = value; OnPropertyChanged(); } }

        public ObservableCollection<BookingDetail> PendingBookingDetails { get; set; }

        private BookingDetail _selectedPendingDetail;
        public BookingDetail SelectedPendingDetail { get => _selectedPendingDetail; set { _selectedPendingDetail = value; OnPropertyChanged(); } }

        private decimal _totalBookingPrice;
        public decimal TotalBookingPrice { get => _totalBookingPrice; set { _totalBookingPrice = value; OnPropertyChanged(); } }

        // --- Commands ---
        public ICommand AddDetailCommand { get; }
        public ICommand RemoveDetailCommand { get; }
        public ICommand CreateBookingCommand { get; }
        public ICommand CancelCommand { get; }

        public BookingViewModel(
            ICustomerRepository customerRepo,
            IBookingReservationRepository bookingRepo,
            IBookingDetailRepository detailRepo,
            IRoomInformationRepository roomRepo)
        {
            _customerRepo = customerRepo;
            _bookingRepo = bookingRepo;
            _detailRepo = detailRepo;
            _roomRepo = roomRepo;

            AvailableRooms = new ObservableCollection<RoomInformation>();
            PendingBookingDetails = new ObservableCollection<BookingDetail>();
            AllCustomers = new ObservableCollection<Customer>();

            AddDetailCommand = new RelayCommand(ExecuteAddDetail, CanExecuteAddDetail);
            RemoveDetailCommand = new RelayCommand(ExecuteRemoveDetail, CanExecuteRemoveDetail);
            CreateBookingCommand = new RelayCommand(ExecuteCreateBooking, CanExecuteCreateBooking);
            CancelCommand = new RelayCommand(Cancel);
        }

        // Dùng để AdminViewModel gọi khi mở cửa sổ
        public void Initialize()
        {
            LoadAvailableRooms();
            LoadAllCustomers();
            PendingBookingDetails.Clear();
            UpdateTotalBookingPrice();
            BookingErrorMessage = "";
        }

        private void LoadAllCustomers()
        {
            var customers = _customerRepo.GetAllCustomers();
            AllCustomers.Clear();
            foreach (var customer in customers)
            {
                AllCustomers.Add(customer);
            }
        }

        private void LoadAvailableRooms()
        {
            var rooms = _roomRepo.GetAvailableRooms();
            AvailableRooms.Clear();
            foreach (var room in rooms)
            {
                AvailableRooms.Add(room);
            }
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

                int totalDays = (int)(NewBookingEndDate.Value - NewBookingStartDate.Value).TotalDays;
                if (totalDays <= 0)
                {
                    BookingErrorMessage = "Booking must be for at least 1 night.";
                    return;
                }

                if (_roomRepo.IsRoomAvailable(SelectedAvailableRoom.RoomId, SelectedCustomer.CustomerId) == false)
                {
                    BookingErrorMessage = "You have booked this room.";
                    return;
                }

                foreach (var booking in PendingBookingDetails)
                {
                    if (booking.RoomId == SelectedAvailableRoom.RoomId)
                    {
                        BookingErrorMessage = "You have selected this room.";
                        return;
                    }
                }

                var detail = new BookingDetail
                {
                    RoomId = SelectedAvailableRoom.RoomId,
                    Room = SelectedAvailableRoom,
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
            return SelectedPendingDetail != null;
        }

        private void ExecuteRemoveDetail(object obj)
        {
            PendingBookingDetails.Remove(SelectedPendingDetail);
            UpdateTotalBookingPrice();
        }

        private bool CanExecuteCreateBooking(object obj)
        {
            return PendingBookingDetails.Count > 0 && SelectedCustomer != null;
        }

        private void ExecuteCreateBooking(object obj)
        {
            if (MessageBox.Show("Are you sure to book these rooms?", "Confirm booking",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    BookingErrorMessage = "";
                    var reservation = new BookingReservation
                    {
                        BookingDate = DateTime.Now,
                        TotalPrice = TotalBookingPrice,
                        CustomerId = SelectedCustomer.CustomerId,
                        BookingStatus = "Active",
                        BookingDetails = PendingBookingDetails
                    };

                    _bookingRepo.Add(reservation);

                    RequestClose?.Invoke();
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
            TotalBookingPrice = PendingBookingDetails.Sum(d => d.ActualPrice.Value);
        }

        private void Cancel(object param)
        {
            RequestClose?.Invoke();
        }
    }
}