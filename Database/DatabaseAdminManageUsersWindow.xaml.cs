using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAdminWindow.xaml
    /// </summary>
    public partial class DatabaseAdminManageUsersWindow : Window
    {
        public DatabaseAdminManageUsersWindow()
        {
            InitializeComponent();

            GetUsers();
        }        

        public void GetUsers(string selectedItem = null)
        {
            UsersBox.Items.Clear();

            List<string> users = DatabaseHandler.GetUsers();

            foreach (string user in users)
            {
                UsersBox.Items.Add(user);
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {            
            DatabaseAdminUserWindow dialog = new DatabaseAdminUserWindow();
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                DatabaseUser user = new DatabaseUser()
                {
                    Name = dialog.GetName(),
                    Password = dialog.GetPassword(),
                    Fullname = dialog.GetFullName(),
                    Email = dialog.Getemail(),
                    Expertise = dialog.GetExpertise()
 
                };


                if (DatabaseHandler.AddUser(user, dialog.GetIsUserAdmin()))
                {
                    GetUsers();
                }
            }
        }


        private void removeAllBountiesFromUser(string username)
        {
            List<string> databases = DatabaseHandler.GetDatabases();
            foreach (string databaseName in databases)
            {
                IMongoDatabase db = DatabaseHandler.Client.GetDatabase(databaseName);
                List<DatabaseBounty> bounties = new List<DatabaseBounty>();
                bounties = DatabaseHandler.LoadAcceptedBounties(db);
                if(bounties != null)
                {
                    foreach (var bounty in bounties)
                    {
                        //accepted task
                        int index = bounty.annotatorsJobCandidates.FindIndex(s => s.Name == username);
                        if (index > -1)
                        {
                            bounty.annotatorsJobCandidates.RemoveAt(index);
                            bounty.numOfAnnotationsNeededCurrent += 1;
                            DatabaseHandler.SaveBounty(bounty);

                        }

                        //contractor
                        if (bounty.Contractor == DatabaseHandler.GetUserInfo(username))
                        {
                            DatabaseHandler.DeleteBounty(bounty.OID);
                        }
                    }
                }
               

            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersBox.SelectedItem != null)
            {
                string user = (string)UsersBox.SelectedItem;
                MessageBoxResult result = MessageBox.Show("Delete user '" + user + "'?", "Question", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                     
                    if (DatabaseHandler.DeleteUser(user))
                    {
                        removeAllBountiesFromUser(user); //make sure 
                        GetUsers();
                    }
                }
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersBox.SelectedItem != null)
            {
                DatabaseUser blankuser = new DatabaseUser()
                {
                    Name = (string)UsersBox.SelectedItem
                };

                blankuser = DatabaseHandler.GetUserInfo(blankuser);


                DatabaseAdminUserWindow dialog = new DatabaseAdminUserWindow(blankuser.Name,blankuser.Fullname,blankuser.Email,blankuser.Expertise);
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    string pass = dialog.GetPassword();

                    DatabaseUser user = new DatabaseUser()
                    {
                        Name = dialog.GetName(),
                        Fullname = dialog.GetFullName(),
                        Email = dialog.Getemail(),
                        Expertise = dialog.GetExpertise(),
                        Password = dialog.GetPassword(),
                        ln_admin_key = blankuser.ln_admin_key,
                        ln_invoice_key = blankuser.ln_invoice_key,
                        ln_wallet_id = blankuser.ln_wallet_id,
                        ln_user_id = blankuser.ln_user_id,
                        ln_addressname = blankuser.ln_addressname,
                        ln_addresspin = blankuser.ln_addresspin,   
                    };



                    if (user.Password != "")
                    {
                        if(user.ln_wallet_id != null || user.ln_wallet_id != "")
                        {
                            MessageBoxResult mb = MessageBox.Show("User has a Lightning Wallet, Changing Password will make it unaccessable if user didn't save admin key!","Attention",MessageBoxButton.OKCancel);
                            if(mb == MessageBoxResult.OK)
                            {
                                DatabaseHandler.ChangeUserPassword(user);
                            }

                        }
                        else if (DatabaseHandler.ChangeUserPassword(user))
                        {
                        
                        }
                    }

                    //if user has no wallet
                    if (user.ln_wallet_id == null || user.ln_wallet_id == "")
                    {
                        user.ln_wallet_id = "";
                        user.ln_user_id = "";
                        user.ln_invoice_key = "";
                        user.ln_admin_key = "";
                        user.ln_addressname = "";
                        user.ln_addresspin = "";
                    }
                  

                    DatabaseHandler.ChangeUserCustomData(user);

                    GetUsers();

                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //DatabaseHandler.UpdateDatabaseLocalLists();
        }
    }
}