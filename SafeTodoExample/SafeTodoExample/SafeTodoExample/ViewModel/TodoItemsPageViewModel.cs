﻿using Rg.Plugins.Popup.Extensions;
using SafeTodoExample.Helpers;
using SafeTodoExample.Model;
using SafeTodoExample.ViewModel.Base;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace SafeTodoExample.ViewModel
{
    public class TodoItemsPageViewModel : BaseViewModel
    {
        public ICommand AddItemCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand RefereshItemCommand { get; }
        public ICommand UpdateItemCommand { get; }
        public ICommand DeleteItemCommand { get; }

        private ObservableCollection<TodoItem> _todoItems;
        public ObservableCollection<TodoItem> ToDoItems
        {
            get { return _todoItems; }
            set
            {
                SetProperty(ref _todoItems, value);
            }
        }

        public TodoItemsPageViewModel()
        {
            AddItemCommand = new Command(async () => await OnAddItemCommand());
            LogoutCommand = new Command(async () => await OnLogoutCommandAsync());
            RefereshItemCommand = new Command(async () => await OnRefreshItemsCommand());
            UpdateItemCommand = new Command(async (item) => await OnUpdateItemsCommand((TodoItem)item));
            DeleteItemCommand = new Command(async (item) => await OnDeleteItemsCommand((TodoItem)item));
            ToDoItems = new ObservableCollection<TodoItem>();
            Device.BeginInvokeOnMainThread(async () => await OnRefreshItemsCommand());
        }

        private async Task OnRefreshItemsCommand()
        {
            IsBusy = true;
            try
            {
                ToDoItems = await AppService.GetItemAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Fetch Ids Failed: {ex.Message}", "OK");
            }

            IsBusy = false;
        }

        private async Task OnAddItemCommand()
        {
            await Application.Current.MainPage.Navigation.PushPopupAsync(new View.AddItem());
        }

        private async Task OnLogoutCommandAsync()
        {
            await AppService.LogoutAsync();
            MessagingCenter.Send(this, MessengerConstants.NavigateToAuthPage);
        }

        private async Task OnDeleteItemsCommand(TodoItem item)
        {
            var result = await Application.Current.MainPage.DisplayAlert("Delete item",
                "Are you sure you want to delete this item from list", "Delete", "Cancel");
            if (result)
            {
                DialogHelper.ShowToast("Deleting entry...", DialogType.Information);

                await AppService.DeleteItemAsync(item);
                if (RefereshItemCommand.CanExecute(null))
                {
                    RefereshItemCommand.Execute(null);
                }
            }
        }

        private async Task OnUpdateItemsCommand(TodoItem item)
        {
            await Application.Current.MainPage.Navigation.PushPopupAsync(new View.AddItem(item));
        }
    }
}
