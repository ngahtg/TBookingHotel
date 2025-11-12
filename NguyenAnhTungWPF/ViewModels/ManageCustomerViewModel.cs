using NguyenAnhTungWPF.Models;
using NguyenAnhTungWPF.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NguyenAnhTungWPF.ViewModels
{
    public class ManageCustomerViewModel : ViewModelBase
    {
        private readonly ICustomerRepository _customerRepository;
        private Customer _currentCustomer = new();

        // Thêm trường này để giữ object gốc
        private Customer _originalCustomer;

        public event Action RequestClose;

        //Bìnding
        public Customer CurrentCustomer { get => _currentCustomer;set { _currentCustomer = value;OnPropertyChanged(); }  }
        
        public DateTime CustomerBirthday
        {
            get =>  CurrentCustomer.CustomerBirthday?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
            set
            {
                CurrentCustomer.CustomerBirthday = DateOnly.FromDateTime(value);
                OnPropertyChanged();
            }
        }

        private string _windowTitle = "";
        public string WindowTitle { get => _windowTitle; set { _windowTitle = value; OnPropertyChanged(); } }

        private string _errorMessage;
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        

        public ManageCustomerViewModel(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
            SaveCommand=new RelayCommand(SaveChanges, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }
        internal void Initilize(int? customerId)
        {
            if (customerId == null)
            {
                WindowTitle = "Add New Customer";
                CurrentCustomer = new Customer { CustomerStatus = "Active" };
            }
            else
            {
                WindowTitle = "Update Customer";
                _originalCustomer = _customerRepository.GetCustomerById(customerId.Value);
                CurrentCustomer = new Customer
                {
                    CustomerId = _originalCustomer.CustomerId,
                    CustomerFullName = _originalCustomer.CustomerFullName,
                    EmailAddress = _originalCustomer.EmailAddress,
                    Telephone = _originalCustomer.Telephone,
                    CustomerBirthday = _originalCustomer.CustomerBirthday,
                    Password = _originalCustomer.Password,
                    CustomerStatus = _originalCustomer.CustomerStatus
                };
                OnPropertyChanged(nameof(CurrentCustomer));
                OnPropertyChanged(nameof(CustomerBirthday));
            }

        }
        private bool CanSave(object? param)
        {
            return !string.IsNullOrWhiteSpace(CurrentCustomer.CustomerFullName) &&
                   !string.IsNullOrWhiteSpace(CurrentCustomer.EmailAddress) &&
                   !string.IsNullOrWhiteSpace(CurrentCustomer.Telephone) &&
                   CurrentCustomer.CustomerBirthday != null &&
                   !string.IsNullOrWhiteSpace(CurrentCustomer.Password) ;
        }
        private void SaveChanges(object? param)
        {
            try
            {
                if(WindowTitle=="Add New Customer")
                {
                    
                    _customerRepository.AddCustomer(CurrentCustomer);
                }
                else
                {
                    _originalCustomer.CustomerFullName = CurrentCustomer.CustomerFullName;
                    _originalCustomer.EmailAddress = CurrentCustomer.EmailAddress;
                    _originalCustomer.Telephone = CurrentCustomer.Telephone;
                    _originalCustomer.CustomerBirthday = CurrentCustomer.CustomerBirthday;
                    _originalCustomer.Password = CurrentCustomer.Password;
                    _customerRepository.UpdateCustomer(_originalCustomer);
                }
                RequestClose?.Invoke();
            } catch (Exception ex)
            {
                ErrorMessage = "Error" + ex.Message;
            }
        }
        private void Cancel(object? param)
        {
            RequestClose?.Invoke();
        }
    }
}
