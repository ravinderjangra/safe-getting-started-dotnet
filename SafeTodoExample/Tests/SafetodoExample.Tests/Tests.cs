using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SafeTodoExample.Helpers;
using SafeTodoExample.ViewModel;
using Xamarin.Forms;

namespace SafetodoExample.Tests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public async Task MockAuthenticationTest()
        {
            try
            {
                bool messageReceived = false;
                var authViewModel = new MainPageViewModel();

                MessagingCenter.Subscribe<MainPageViewModel>(
                    this, MessengerConstants.NavigateToItemPage, sender =>
                    {
                        messageReceived = true;
                    });

                await authViewModel.ConnectToMockAsync();
                Assert.True(messageReceived);
                MessagingCenter.Unsubscribe<MainPageViewModel>(this, MessengerConstants.NavigateToAuthPage);

                authViewModel.AppService.Dispose();
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.Message);
            }
        }

        [Test]
        public async Task MutableOperationsTest()
        {
            try
            {
                bool messageReceived = false;
                var authViewModel = new MainPageViewModel();

                MessagingCenter.Subscribe<MainPageViewModel>(
                    this, MessengerConstants.NavigateToItemPage, sender =>
                    {
                        messageReceived = true;
                    });

                await authViewModel.ConnectToMockAsync();
                MessagingCenter.Unsubscribe<MainPageViewModel>(this, MessengerConstants.NavigateToAuthPage);

                Assert.True(messageReceived);

                var todoItemsViewModel = new TodoItemsPageViewModel();

                // Test get mdata entries
                await todoItemsViewModel.OnRefreshItemsCommand();
                Assert.Zero(todoItemsViewModel.ToDoItems.Count);

                var addItemViewModel = new AddItemViewModel();

                // Test add todo item
                messageReceived = false;
                MessagingCenter.Subscribe<AddItemViewModel>(
                    this, MessengerConstants.RefreshItemList, sender =>
                    {
                        messageReceived = true;
                    });
                addItemViewModel.Title = Misc.GetRandomString(10);
                addItemViewModel.Details = Misc.GetRandomString(10);

                await addItemViewModel.OnSaveItemCommand();
                Assert.True(messageReceived, "Adding entry failed");

                // Test fetch todo items
                await todoItemsViewModel.OnRefreshItemsCommand();
                Assert.NotZero(todoItemsViewModel.ToDoItems.Count);

                // Test add second todo item
                messageReceived = false;
                addItemViewModel.Title = Misc.GetRandomString(10);
                addItemViewModel.Details = Misc.GetRandomString(10);

                await addItemViewModel.OnSaveItemCommand();
                Assert.True(messageReceived, "Adding entry failed");

                // Test fetch todo items
                await todoItemsViewModel.OnRefreshItemsCommand();
                Assert.NotZero(todoItemsViewModel.ToDoItems.Count);
                Assert.AreEqual(2, todoItemsViewModel.ToDoItems.Count);

                // Test delete todo item
                await addItemViewModel.DeleteItemAsync(todoItemsViewModel.ToDoItems[0]);

                await todoItemsViewModel.OnRefreshItemsCommand();
                Assert.AreEqual(1, todoItemsViewModel.ToDoItems.Count);

                // Test update todo item
                var updateViewModel = new AddItemViewModel(todoItemsViewModel.ToDoItems[0], true);
                var newDetails = "UpdatedDetails";
                updateViewModel.Details = newDetails;

                await updateViewModel.OnSaveItemCommand();
                await todoItemsViewModel.OnRefreshItemsCommand();
                Assert.AreEqual(1, todoItemsViewModel.ToDoItems.Count);
                Assert.AreEqual(newDetails, todoItemsViewModel.ToDoItems[0].Detail);

                MessagingCenter.Unsubscribe<AddItemViewModel>(this, MessengerConstants.RefreshItemList);
                authViewModel.AppService.Dispose();
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.Message);
            }
        }
    }
}
