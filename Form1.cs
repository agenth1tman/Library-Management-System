using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using System.Data;
using Microsoft.VisualBasic;
using System.DirectoryServices;

namespace Library_Management_System
{
    public partial class Form1 : Form
    {
        //Book Variables
        public List<Book> Books = new List<Book>();
        int totalBooks;
        int availableBooks;
        int issuedBooks;
        bool issueUIReady = false;

        //UI References
        List<Panel> panelList = new List<Panel>();
        List<Button> buttonList = new List<Button>();

        //Other Variables
        public DataTable dataTable = new DataTable();

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            //Initialize App
            try
            {
                UseWaitCursor = true;
                InitializeFirebase();
                Start();
                await GetBooksFromFirebase();
                LoadDataGrid();
                PanelSwitcher(HomePanel, HomeButton);
                ToggleButtons(true);
                UseWaitCursor = false;
            }
            //Display Error Message
            catch
            {
                UseWaitCursor = false;
                DialogResult result = MessageBox.Show("Connection to the server can not be stablished", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }
            }
        }
        public void InitializeFirebase()
        {
            string credentialsPath = @"D:\Visual Studio\Projects\Library Management System\Library Management System\librarymanagementsystem-6d880-firebase-adminsdk-y46af-0a973e447f.json";
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")))
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
            }

            // Attempt to create the FirebaseApp
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(credentialsPath)
            });

        }

        void Start()
        {
            AddEveryPanelInList();
            AddEveryButtonInList();
            ToggleButtons(false);
            PanelSwitcher(LoadingPanel, HomeButton);
        }

        #region UI References
        private void AddEveryPanelInList()
        {
            panelList.Add(HomePanel);
            panelList.Add(LibraryPanel);
            panelList.Add(LoadingPanel);
            panelList.Add(RegisterPanel);
            panelList.Add(IssuePanel);
        }

        void AddEveryButtonInList()
        {
            buttonList.Add(HomeButton);
            buttonList.Add(LibraryButton);
            buttonList.Add(RegisterButton);
            buttonList.Add(IssueButton);
        }

        void PanelSwitcher(Panel designatedPanel, Button optionButton)
        {
            //Enable Designated Panel
            designatedPanel.Enabled = true;
            designatedPanel.Visible = true;

            //Disable Others
            foreach (Panel panel in panelList)
            {
                if (panel != designatedPanel)
                {
                    panel.Enabled = false;
                    panel.Visible = false;
                }
            }
            if (optionButton != null)
            {
                //Put Selected Color on Designated Button
                optionButton.BackColor = Color.Silver;
                optionButton.ForeColor = Color.Black;

                //Put Deselected Color on Other Buttons
                foreach (Button button in buttonList)
                {
                    if (button != optionButton)
                    {
                        button.BackColor = Color.FromArgb(35, 35, 35);
                        button.ForeColor = Color.White;
                    }
                }
            }
        }

        void ToggleButtons(bool toggleState)
        {
            foreach (Button button in buttonList)
            {
                button.Enabled = toggleState;
            }
        }
        #endregion

        #region ServerInteractionCode
        public async Task GetBooksFromFirebase()
        {
            FirestoreDb db = FirestoreDb.Create("librarymanagementsystem-6d880");
            CollectionReference booksRef = db.Collection("Books");
            QuerySnapshot snapshot = await booksRef.GetSnapshotAsync();

            Books.Clear();
            totalBooks = 0;
            availableBooks = 0;
            issuedBooks = 0;
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                Book book = new Book();
                book.ID = document.Id;
                book.Name = document.GetValue<string>("Name");
                book.Author = document.GetValue<string>("Author");
                book.Available = document.GetValue<bool>("Available");
                book.Barcode = document.GetValue<string>("Barcode");
                book.IssueDate = document.GetValue<string>("IssueDate");
                book.RegistrationDate = document.GetValue<string>("RegistrationDate");
                book.IssuedBy = document.GetValue<string>("IssuedBy");

                Books.Add(book);
                totalBooks++;
            }
        }

        public async Task RefreshBooks()
        {
            try
            {
                GV_Database.Rows.Clear();
                UseWaitCursor = true;
                await GetBooksFromFirebase();
                LoadDataGrid();
                UseWaitCursor = false;
            }
            catch
            {
                UseWaitCursor = false;
                DialogResult result = MessageBox.Show("Connection to the server can not be stablished", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }
            }
        }
        public async Task AddBookAsync(string name, string author, string barcode, string registrationDate, bool available)
        {
            //Add new book
            try
            {
                UseWaitCursor = true;
                // Create an instance of FirestoreDb
                FirestoreDb db = FirestoreDb.Create("librarymanagementsystem-6d880");

                // Create a reference to the "Books" collection
                CollectionReference booksRef = db.Collection("Books");

                int bookCount = 0;
                foreach (Book book in Books)
                {
                    if (int.Parse(book.ID) != bookCount)
                    {
                        break;
                    }
                    bookCount++;
                }

                //Create a new document with the specified ID(refer to it if already created)
                DocumentReference docRef = booksRef.Document((bookCount).ToString());

                // Define the data to add (as a dictionary)
                Dictionary<string, object> bookData = new Dictionary<string, object>()
            {
                { "Name", name },
                { "Author", author },
                { "Barcode", barcode },
                { "Available",  available},
                { "IssueDate", "null" },
                { "RegistrationDate", registrationDate },
                { "IssuedBy", "null" },
                };

                // Add the data to the document
                await docRef.SetAsync(bookData);
                UseWaitCursor = false;
            }
            //Display Error Message if it fails
            catch
            {
                UseWaitCursor = false;
                DialogResult result = MessageBox.Show("Connection to the server can not be established", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }
            }
        }

        public async Task UpdateIssue(int id, string? issuedby, bool available)
        {
            //Update Availibility
            try
            {
                UseWaitCursor = true;
                // Create an instance of FirestoreDb
                FirestoreDb db = FirestoreDb.Create("librarymanagementsystem-6d880");

                // Create a reference to the "Books" collection
                CollectionReference booksRef = db.Collection("Books");

                // Create a new document with the specified ID(refer to it if already created)
                DocumentReference docRef = booksRef.Document((id).ToString());


                await docRef.UpdateAsync("Available", available);
                if (available)
                {
                    await docRef.UpdateAsync("IssuedBy", "null");
                    await docRef.UpdateAsync("IssueDate", "null");
                }
                else
                {
                    string dateString = DateTime.Now.ToString("dd-MM-yyyy");
                    await docRef.UpdateAsync("IssuedBy", issuedby);
                    await docRef.UpdateAsync("IssueDate", dateString);
                }
                UseWaitCursor = false;
            }
            //Display error message if fails
            catch
            {
                UseWaitCursor = false;
                DialogResult result = MessageBox.Show("Connection to the server can not be established", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }
            }
        }

        public async Task DeleteEntry(int id)
        {
            //Delete
            try
            {
                UseWaitCursor = true;
                // Create an instance of FirestoreDb
                FirestoreDb db = FirestoreDb.Create("librarymanagementsystem-6d880");

                // Create a reference to the "Books" collection
                CollectionReference booksRef = db.Collection("Books");

                // Create a new document with the specified ID(refer to it if already created)
                DocumentReference docRef = booksRef.Document((id).ToString());

                await docRef.DeleteAsync();
                UseWaitCursor = false;
            }

            catch
            {
                UseWaitCursor = false;
                DialogResult result = MessageBox.Show("Connection to the server can not be established", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }
            }
        }

        public async Task EditEntry(int id, string bookName, string author, string barcode, string registration,string? issuedDate,string? issuedby, bool available)
        {
            //Update Availibility
            try
            {
                UseWaitCursor = true;
                // Create an instance of FirestoreDb
                FirestoreDb db = FirestoreDb.Create("librarymanagementsystem-6d880");

                // Create a reference to the "Books" collection
                CollectionReference booksRef = db.Collection("Books");

                // Create a new document with the specified ID(refer to it if already created)
                DocumentReference docRef = booksRef.Document((id).ToString());

                await docRef.UpdateAsync("Name", bookName);
                await docRef.UpdateAsync("Author", author);
                await docRef.UpdateAsync("Barcode", barcode);
                await docRef.UpdateAsync("RegistrationDate", registration);

                if (!available)
                {
                    await docRef.UpdateAsync("IssuedBy", issuedby);
                    await docRef.UpdateAsync("IssueDate", issuedDate);
                }
                UseWaitCursor = false;
            }
            //Display error message if fails
            catch
            {
                UseWaitCursor = false;
                DialogResult result = MessageBox.Show("Connection to the server can not be established", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }
            }
        }
        #endregion

        #region LocalCode
        private void LoadDataGrid()
        {
            //Format Grid
            GV_Database.AutoGenerateColumns = false;
            GV_Database.Rows.Clear();

            //Populate the Grid
            foreach (Book book in Books)
            {
                GV_Database.Rows.Add(book.ID, book.Name, book.Author, book.Barcode, book.Available);
                if (book.Available)
                {
                    availableBooks++;
                }
                else { issuedBooks++; }
            }

            //Load Text
            TB_TotalBooks.Text = totalBooks.ToString();
            TB_AvailableBooks.Text = availableBooks.ToString();
            TB_IssuedBooks.Text = issuedBooks.ToString();
        }
        void Search()
        {
            string searchValue = RTB_Search.Text.Trim().ToLower();

            //When Search box is empty load all
            if (searchValue == "")
            {
                RTB_Search.Text = string.Empty;
                GV_Database.Rows.Clear();

                foreach (Book book in Books)
                {
                    GV_Database.Rows.Add(book.ID, book.Name, book.Author, book.Barcode, book.Available);
                }
                return;
            }

            // Filtered list
            var filteredData = Books.Where(book =>
                book.Name.ToLower().Contains(searchValue) ||
                book.Author.ToLower().Contains(searchValue) ||
                book.ID.ToLower().Contains(searchValue) ||
                book.Barcode.ToLower().Contains(searchValue)// Add more properties as needed
            ).ToList();

            // Clear the Grid and repopulate with the filtered data
            GV_Database.Rows.Clear();
            foreach (var book in filteredData)
            {
                GV_Database.Rows.Add(ConvertObjectToRow(book));
            }
        }
        // Helper method to convert an object to a DataGridViewRow
        private DataGridViewRow ConvertObjectToRow(Book obj)
        {
            DataGridViewRow row = new DataGridViewRow();
            row.CreateCells(GV_Database);
            row.Cells[0].Value = obj.ID;
            row.Cells[1].Value = obj.Name;
            row.Cells[2].Value = obj.Author;
            row.Cells[3].Value = obj.Barcode;
            row.Cells[4].Value = obj.Available;
            // Add more properties as needed
            return row;
        }

        void ViewBook(string id)
        {
            RTB_BookID.Text = id;
            CheckInfoFromID();
            PanelSwitcher(IssuePanel, IssueButton);
            RTB_BookID.Focus();
        }
        void CheckInfoFromID()
        {
            //Format Checking
            if (String.IsNullOrWhiteSpace(RTB_BookID.Text))
            {
                MessageBox.Show("Book ID not found!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            int id;
            try
            {
                id = int.Parse(RTB_BookID.Text.Trim());
            }
            catch
            {
                MessageBox.Show("Please Enter a number", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (id > Books.Count - 1 || id < 0)
            {
                MessageBox.Show("Please Enter the correct Book ID", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Checking Info
            bool bookAvailable = Books[id].Available;

            //Writing Info in UI
            if (bookAvailable)
            {
                ReturnContainer.Visible = false;
                ReturnContainer.Enabled = false;

                RTBa_BookName.Text = Books[id].Name;
                RTBa_AuthorName.Text = Books[id].Author;
                RTBa_Barcode.Text = Books[id].Barcode;
                RTBa_RegistrationDate.Text = Books[id].RegistrationDate;

                IssueContainer.Visible = true;
                IssueContainer.Enabled = true;
                issueUIReady = true;
            }
            else
            {
                IssueContainer.Visible = false;
                IssueContainer.Enabled = false;

                RTBi_BookName.Text = Books[id].Name;
                RTBi_AuthorName.Text = Books[id].Author;
                RTBi_Barcode.Text = Books[id].Barcode;
                RTBi_RegistrationDate.Text = Books[id].RegistrationDate;
                RTBi_IssueDate.Text = Books[id].IssueDate;
                RTBi_IssuedTo.Text = Books[id].IssuedBy;

                ReturnContainer.Visible = true;
                ReturnContainer.Enabled = true;
                issueUIReady = true;
            }
        }

        void RemoveInfo()
        {
            ReturnContainer.Visible = false;
            ReturnContainer.Enabled = false;
            IssueContainer.Visible = false;
            IssueContainer.Enabled = false;
            issueUIReady = false;

            label16.Text = "Book Name: ";
            label17.Text = "Author's Name: ";
            label15.Text = "Barcode: ";
            label18.Text = "Registration Date: ";

            label22.Text = "Book Name: ";
            label24.Text = "Author's Name: ";
            label23.Text = "Barcode: ";
            label21.Text = "Registration Date: ";
            label20.Text = "Issue Date: ";
            label26.Text = "Issued to: ";
        }

        async void CmdUpdateIssue()
        {
            if (!issueUIReady) { return; }
            int id = int.Parse(RTB_BookID.Text.Trim());

            try
            {
                UseWaitCursor = true;
                if (Books[id].Available)
                {
                    //Issue Book
                    await UpdateIssue(id, RTB_Username.Text, !Books[id].Available);
                }
                else
                {
                    //Return Book
                    await UpdateIssue(id, null, !Books[id].Available);
                }
                UseWaitCursor = false;
                RemoveInfo();
                MessageBox.Show("The Entry has been updated!", "Success", MessageBoxButtons.OK);
                RefreshBooks();
                RTB_Username.Text = "";
            }
            catch
            {
                UseWaitCursor = false;
            }
        }

        async void CmdDeleteEntry(int id)
        {
            if (!issueUIReady) { return; }

            //Give last chance to user
            DialogResult r = MessageBox.Show("Do you want to delete this entry?", "Alert!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.No)
            {
                return;
            }

            try
            {
                await DeleteEntry(id);
                RemoveInfo();
                MessageBox.Show("The Entry has been Deleted!", "Success", MessageBoxButtons.OK);
                RefreshBooks();
                RTB_BookID.Text = "";
            }
            catch
            {
                UseWaitCursor = false;
            }

        }

        async void CmdEditEntry()
        {
            if (!issueUIReady) { return; }
            int id = int.Parse(RTB_BookID.Text.Trim());
            
            DialogResult r = MessageBox.Show("Do you want to Edit this entry?", "Alert!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.No)
            {
                if (Books[id].Available)
                {
                    //Available Book
                    RTBa_BookName.Text = Books[id].Name;
                    RTBa_AuthorName.Text = Books[id].Author;
                    RTBa_Barcode.Text = Books[id].Barcode;
                    RTBa_RegistrationDate.Text = Books[id].RegistrationDate;
                }
                else
                {
                    //Issued Book
                    RTBi_BookName.Text = Books[id].Name;
                    RTBi_AuthorName.Text = Books[id].Author;
                    RTBi_Barcode.Text = Books[id].Barcode;
                    RTBi_RegistrationDate.Text = Books[id].RegistrationDate;
                    RTBi_IssueDate.Text = Books[id].IssueDate;
                    RTBi_IssuedTo.Text = Books[id].IssuedBy;
                }
                return;
            }

            try
            {
                UseWaitCursor = true;
                if (Books[id].Available)
                {
                    //Available Book
                    await EditEntry(id, RTBa_BookName.Text.Trim(), RTBa_AuthorName.Text.Trim(), RTBa_Barcode.Text.Trim(), RTBa_RegistrationDate.Text.Trim(), null,  null, Books[id].Available);
                }
                else
                {
                    //Issued Book
                    await EditEntry(id, RTBi_BookName.Text.Trim(), RTBi_AuthorName.Text.Trim(), RTBi_Barcode.Text.Trim(), RTBi_RegistrationDate.Text.Trim(), RTBi_IssueDate.Text.Trim(), RTBi_IssuedTo.Text.Trim(), Books[id].Available);
                }
                UseWaitCursor = false;
                RemoveInfo();
                MessageBox.Show("The Entry has been Editted!", "Success", MessageBoxButtons.OK);
                RefreshBooks();
            }
            catch
            {
                UseWaitCursor = false;
            }
        }
        #endregion

        #region UI Clicks
        private void HomeButton_Click(object sender, EventArgs e)
        {
            PanelSwitcher(HomePanel, HomeButton);
        }

        private void LibraryButton_Click(object sender, EventArgs e)
        {
            PanelSwitcher(LibraryPanel, LibraryButton);
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            PanelSwitcher(RegisterPanel, RegisterButton);
        }

        private void IssueButton_Click(object sender, EventArgs e)
        {
            PanelSwitcher(IssuePanel, IssueButton);
        }

        private async void B_Register_Click(object sender, EventArgs e)
        {
            //Check if empty
            if (String.IsNullOrWhiteSpace(RTB_Name.Text) || String.IsNullOrWhiteSpace(RTB_Author.Text) || String.IsNullOrWhiteSpace(RTB_Barcode.Text))
            {
                MessageBox.Show("Any Fields cannot be left empty", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Register
            try
            {
                string dateString = DateTime.Now.ToString("dd-MM-yyyy");

                var bookData = new
                {
                    Name = RTB_Name.Text.Trim(),
                    Author = RTB_Author.Text.Trim(),
                    Barcode = RTB_Barcode.Text.Trim(),
                    Available = true,
                    RegistrationDate = dateString
                };

                await AddBookAsync(bookData.Name, bookData.Author, bookData.Barcode, bookData.RegistrationDate, bookData.Available);
                await GetBooksFromFirebase();
                LoadDataGrid();
                MessageBox.Show("This Book is Registered!");
                RTB_Name.Text = "";
                RTB_Author.Text = "";
                RTB_Barcode.Text = "";
            }
            //Display Error Message
            catch
            {
                DialogResult result = MessageBox.Show("Connection to the server can not be established", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }
            }
        }

        private void B_Search_Click(object sender, EventArgs e)
        {
            Search();
        }

        private void RTB_Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Search();
            }
        }

        private void B_Refresh_Click(object sender, EventArgs e)
        {
            RefreshBooks();
        }

        private void GV_Database_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == GV_Database.Columns[5].Index && e.RowIndex >= 0)
            {
                //View Button Clicked
                string value = GV_Database.Rows[e.RowIndex].Cells[0].Value.ToString();
                ViewBook(value);
            }
        }

        private void B_GetInfo_Click(object sender, EventArgs e)
        {
            CheckInfoFromID();
        }
        private void RTB_BookID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { CheckInfoFromID(); }
        }

        private void B_Issue_Click(object sender, EventArgs e)
        {
            //Check if empty
            if (String.IsNullOrWhiteSpace(RTB_Username.Text))
            {
                MessageBox.Show("Any Fields cannot be left empty", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Command to Update
            CmdUpdateIssue();
        }

        private void B_Return_Click(object sender, EventArgs e)
        {
            CmdUpdateIssue();
        }

        private void Bi_Delete_Click(object sender, EventArgs e)
        {
            int id = int.Parse(RTB_BookID.Text.Trim());
            CmdDeleteEntry(id);
        }

        private void Ba_Delete_Click(object sender, EventArgs e)
        {
            int id = int.Parse(RTB_BookID.Text.Trim());
            CmdDeleteEntry(id);
        }

        private void Bi_Edit_Click(object sender, EventArgs e)
        {
            //Check if empty
            if (String.IsNullOrWhiteSpace(RTBi_BookName.Text) || String.IsNullOrWhiteSpace(RTBi_AuthorName.Text) || String.IsNullOrWhiteSpace(RTBi_Barcode.Text)
                || String.IsNullOrWhiteSpace(RTBi_RegistrationDate.Text) || String.IsNullOrWhiteSpace(RTBi_IssueDate.Text)
                || String.IsNullOrWhiteSpace(RTBi_IssuedTo.Text))
            {
                MessageBox.Show("Any Fields cannot be left empty", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CmdEditEntry();
        }

        private void Ba_Edit_Click(object sender, EventArgs e)
        {
            //Check if empty
            if (String.IsNullOrWhiteSpace(RTBa_BookName.Text) || String.IsNullOrWhiteSpace(RTBa_AuthorName.Text) || String.IsNullOrWhiteSpace(RTBa_Barcode.Text)
                || String.IsNullOrWhiteSpace(RTBa_RegistrationDate.Text))
            {
                MessageBox.Show("Any Fields cannot be left empty", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CmdEditEntry();
        }

        #endregion

        #region Debugging
        private void button1_Click(object sender, EventArgs e)
        {
            int b = Books.Count;
            int a;
            try { a = int.Parse(RTB_Search.Text); }
            catch { return; }
            for (int i = 0; i < a; i++)
            {
                b++;
                GV_Database.Rows.Add(b, "TestName", "TestAuthor", false);
            }
        }
        #endregion
    }
}
