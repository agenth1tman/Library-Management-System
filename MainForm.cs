using FirebaseAdmin;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using System.Data;
using System.Drawing.Text;
using BasselTech.UsbBarcodeScanner;
using System.Diagnostics;

namespace Library_Management_System
{
    public partial class MainForm : Form
    {
        // Constants
        const string project_id = "librarymanagementsystem-6d880";
        string credentialsPath = @"D:\Visual Studio\Projects\Library Management System\Library Management System\librarymanagementsystem-6d880-firebase-adminsdk-y46af-0a973e447f.json";

        UIManager UI_Manager = new UIManager();
        FontManager Font_Manager = new FontManager();
        PrivateFontCollection pfc;
        ExportToExcel exportToExcel = new ExportToExcel();

        //User Variables
        public List<User> Users = new List<User>();

        //Book Variables
        public List<Book> Books = new List<Book>();
        public List<Book> UpcomingDeadline_Books = new List<Book>();
        public List<Book> Overdue_Books = new List<Book>();

        //Reports Variables
        bool isDailyReportGenerated = false;
        int totalBooks;
        int availableBooks;
        int issuedBooks;
        int issuedtoTeachers;
        int issuedtoStudents;
        int overdueBooks;
        int upcomingDeadlineBooks;

        bool issueUIReady = false;

        //UI References
        List<Panel> panelList = new List<Panel>();
        List<Button> buttonList = new List<Button>();

        //Settings Variables
        public bool EnableAutoShowReports;
        public bool EnableAutoSaveReports;
        public string PreviousSaveDate;

        //Other Variables
        public DataTable dataTable = new DataTable();
        UsbBarcodeScanner scanner = new UsbBarcodeScanner();

        public MainForm()
        {
            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            //Initialize App
            try
            {
                UseWaitCursor = true;
                HomePanel.Focus();
                Font_Manager.Initialize(this);
                ApplyFonts();

                InitializeFirebase();
                Start();
                LoadSettings();
                await GetBooksFromFirebase();
                await GetUsersFromFirebase();
                LoadDataGrid();
                GenerateDailyReport();
                if (EnableAutoShowReports && PreviousSaveDate != DateTime.Now.ToString("dd-MM-yyyy"))
                {
                    PanelSwitcher(DailyReportPanel);
                }
                else
                {
                    PanelSwitcher(HomePanel);
                }
                if (EnableAutoSaveReports) 
                {
                    AutoSaveDailyReport();
                }
                ToggleButtons(true);
                UseWaitCursor = false;

                //scanner.BarcodeScanned += (sender, args) => Scanned(args);
                //scanner.Start();
            }
            //Display Error Message
            catch
            {
                UseWaitCursor = false;
                DialogResult result = MessageBox.Show("Connnnection to the server can not be established", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }
            }
        }

        public void InitializeFirebase()
        {
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
            PanelSwitcher(LoadingPanel);
        }

        void Scanned(BarcodeScannedEventArgs args)
        {
            string barcode = args.Barcode.ToString().Trim();
            string bookID;

            bool isFound = false;
            foreach (Book book in Books)
            {
                if (barcode == book.Barcode)
                {
                    isFound = true;
                    bookID = book.ID;
                    break;
                }
            }
            if (!isFound)
            {
                MessageBox.Show("The scanned Barcode doesn't exist in the database", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            RTB_Search.Text = barcode;
            Search();
            PanelSwitcher(LibraryPanel);
        }

        #region UI References
        private void AddEveryPanelInList()
        {
            panelList.Add(HomePanel);
            panelList.Add(LibraryPanel);
            panelList.Add(LoadingPanel);
            panelList.Add(RegisterPanel);
            panelList.Add(IssuePanel);
            panelList.Add(SettingsPanel);
            panelList.Add(DailyReportPanel);

        }

        void AddEveryButtonInList()
        {
            buttonList.Add(HomeButton);
            buttonList.Add(LibraryButton);
            buttonList.Add(SettingsButton);
            buttonList.Add(ManageButton);
            buttonList.Add(RegisterButton);
        }

        void PanelSwitcher(Panel designatedPanel)
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
        }

        void ToggleButtons(bool toggleState)
        {
            foreach (Button button in buttonList)
            {
                button.Enabled = toggleState;
                button.Visible = toggleState;
            }
        }
        #endregion

        #region ServerInteractionCode
        public async Task GetBooksFromFirebase()
        {
            FirestoreDb db = FirestoreDb.Create(project_id);
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
                book.DueDate = document.GetValue<string>("DueDate");
                book.Barcode_BTSCID = document.GetValue<string>("BTSID");
                book.IssuedBy_BTSCID = document.GetValue<string>("IssuedBy_BTSCID");
                book.IssuedBy_Level = document.GetValue<string>("IssuedBy_Level");
                book.IssuedBy_Section = document.GetValue<string>("IssuedBy_Section");

                Books.Add(book);
                totalBooks++;
            }
        }

        public async Task GetUsersFromFirebase()
        {
            FirestoreDb db = FirestoreDb.Create(project_id);
            CollectionReference booksRef = db.Collection("Users");
            QuerySnapshot snapshot = await booksRef.GetSnapshotAsync();

            Users.Clear();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                User user = new User();
                user.Name = document.GetValue<string>("Name");
                user.BTSCID = document.GetValue<string>("BTSCID");
                user.Level = document.GetValue<string>("Level");
                user.Section = document.GetValue<string>("Section");

                Users.Add(user);
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
                DialogResult result = MessageBox.Show("Connection to the server can not be established", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (result == DialogResult.OK)
                {
                    Application.Exit();
                }
            }
        }
        public async Task AddBookAsync(string name, string author, string barcode, string registrationDate, bool available, string btsID)
        {
            //Add new book
            try
            {
                UseWaitCursor = true;
                // Create an instance of FirestoreDb
                FirestoreDb db = FirestoreDb.Create(project_id);

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
                { "BTSID", btsID },
                { "DueDate", "null" },
                { "IssuedBy_BTSCID", "null" },
                { "IssuedBy_Level", "null" },
                { "IssuedBy_Section", "null" },

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

        public async Task UpdateIssue(int id, string? issuedby, string? issuedby_BTS, string? issuedby_Section, string? issuedby_Level, bool available)
        {
            //Update Availibility
            try
            {
                UseWaitCursor = true;
                // Create an instance of FirestoreDb
                FirestoreDb db = FirestoreDb.Create(project_id);

                // Create a reference to the "Books" collection
                CollectionReference booksRef = db.Collection("Books");

                // Create a new document with the specified ID(refer to it if already created)
                DocumentReference docRef = booksRef.Document((id).ToString());

                await docRef.UpdateAsync("Available", available);
                if (available)
                {
                    await docRef.UpdateAsync("IssuedBy", "null");
                    await docRef.UpdateAsync("IssueDate", "null");
                    await docRef.UpdateAsync("IssuedBy_BTSCID", "null");
                    await docRef.UpdateAsync("IssuedBy_Level", "null");
                    await docRef.UpdateAsync("IssuedBy_Section", "null");
                    await docRef.UpdateAsync("DueDate", "null");
                }
                else
                {
                    string dateString = DateTime.Now.ToString("dd-MM-yyyy");
                    string dueDateString = "";

                    if (issuedby_Level == "Student")
                    {
                        dueDateString = DateTime.Now.AddMonths(1).ToString("dd-MM-yyyy");
                    }
                    else if (issuedby_Level == "Teacher")
                    {
                        dueDateString = "25-06-" + DateTime.Now.Year;
                    }
                    await docRef.UpdateAsync("IssuedBy", issuedby);
                    await docRef.UpdateAsync("IssueDate", dateString);
                    await docRef.UpdateAsync("IssuedBy_BTSCID", issuedby_BTS);
                    await docRef.UpdateAsync("IssuedBy_Level", issuedby_Level);
                    await docRef.UpdateAsync("IssuedBy_Section", issuedby_Section);
                    await docRef.UpdateAsync("DueDate", dueDateString);
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
                FirestoreDb db = FirestoreDb.Create(project_id);

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

        public async Task EditEntry(int id, string bookName, string author, string barcode, string registration, string? issuedDate, string? issuedby, string? issuedby_BTSID, string? issuedby_Level, string? issuedBy_Section, bool available)
        {
            //Update Availibility
            try
            {
                UseWaitCursor = true;
                // Create an instance of FirestoreDb
                FirestoreDb db = FirestoreDb.Create(project_id);

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
                    await docRef.UpdateAsync("IssuedBy_BTSCID", issuedby_BTSID);
                    await docRef.UpdateAsync("IssuedBy_Level", issuedby_Level);
                    await docRef.UpdateAsync("IssuedBy_Section", issuedBy_Section);
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
                string issueDate = "";
                if (book.IssueDate == "null")
                {
                    issueDate = "Not Issued";
                }
                else
                {
                    issueDate = book.IssueDate;
                }
                string dueDate = "";
                if (book.DueDate == "null")
                {
                    dueDate = "Not Issued";
                }
                else
                {
                    dueDate = book.DueDate;
                }
                string issuedto = "";
                if (book.IssuedBy == "null")
                {
                    issuedto = "Not Issued";
                }
                else
                {
                    issuedto = book.IssuedBy + "[" + book.IssuedBy_BTSCID + "]";
                }

                GV_Database.Rows.Add(book.ID, book.Name, book.Author, book.Barcode, book.Barcode_BTSCID, book.RegistrationDate, issueDate, dueDate, issuedto, book.Available);
                if (book.Available)
                {
                    availableBooks++;
                }
                else
                {
                    issuedBooks++;
                    if (book.IssuedBy_Level == "Teacher")
                    {
                        issuedtoTeachers++;
                    }
                    else if (book.IssuedBy_Level == "Student")
                    {
                        issuedtoStudents++;
                    }

                    DateTime parsedDate = DateTime.Parse(book.DueDate);

                    if (DateTime.Today > parsedDate)
                    {
                        overdueBooks++;
                        Overdue_Books.Add(book);
                    }
                    else if (DateTime.Today >= parsedDate.AddDays(-3) && DateTime.Today < parsedDate)
                    {
                        upcomingDeadlineBooks++;
                        UpcomingDeadline_Books.Add(book);
                    }
                }
            }
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
                    string issueDate = "";
                    if (book.IssueDate == "null")
                    {
                        issueDate = "Not Issued";
                    }
                    else
                    {
                        issueDate = book.IssueDate;
                    }

                    string dueDate = "";
                    if (book.DueDate == "null")
                    {
                        dueDate = "Not Issued";
                    }
                    else
                    {
                        dueDate = book.DueDate;
                    }

                    string issuedto = "";
                    if (book.IssuedBy == "null")
                    {
                        issuedto = "Not Issued";
                    }
                    else
                    {
                        issuedto = book.IssuedBy + "[" + book.IssuedBy_BTSCID + "]";
                    }

                    GV_Database.Rows.Add(book.ID, book.Name, book.Author, book.Barcode, book.Barcode_BTSCID, book.RegistrationDate, issueDate, dueDate, issuedto, book.Available);
                }
                return;
            }

            // Filtered list
            var filteredData = Books.Where(book =>
                book.Name.ToLower().Contains(searchValue) ||
                book.Author.ToLower().Contains(searchValue) ||
                book.ID.ToLower().Contains(searchValue) ||
                book.Barcode.ToLower().Contains(searchValue) ||
                book.Barcode_BTSCID.ToLower().Contains(searchValue) ||
                book.RegistrationDate.ToLower().Contains(searchValue) ||
                book.IssueDate.ToLower().Contains(searchValue) ||
                book.DueDate.ToLower().Contains(searchValue) ||
                book.IssuedBy.ToLower().Contains(searchValue) ||
                book.IssuedBy_BTSCID.ToLower().Contains(searchValue)
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
            row.Cells[4].Value = obj.Barcode_BTSCID;
            row.Cells[5].Value = obj.RegistrationDate;
            row.Cells[7].Value = obj.DueDate;
            if (obj.IssueDate == "null") { row.Cells[6].Value = "Not Issued"; }
            else
            {
                row.Cells[6].Value = obj.IssueDate;
            }
            string issuedto = "";
            if (obj.IssuedBy == "null")
            {
                issuedto = "Not Issued";
            }
            else
            {
                issuedto = obj.IssuedBy + "[" + obj.IssuedBy_BTSCID + "]";
            }

            row.Cells[8].Value = issuedto;
            row.Cells[9].Value = obj.Available;
            // Add more properties as needed
            return row;
        }

        void ViewBook(string id)
        {
            RTB_BookID.Text = id;
            CheckInfoFromID();
            PanelSwitcher(IssuePanel);
            RTB_BookID.Focus();
        }
        void CheckInfoFromID()
        {
            //Format Checking
            if (String.IsNullOrWhiteSpace(RTB_BookID.Text))
            {
                //MessageBox.Show("Book ID not found!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!int.TryParse(RTB_BookID.Text.Trim(), out int iiiddd))
            {
                MessageBox.Show("Please enter a valid numeric Book ID", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                AvailabilityStatus.Text = "Available";
                AvailabilityStatus.ForeColor = Color.FromArgb(0, 244, 75);

                RTBi_BookName.Text = Books[id].Name;
                RTBi_AuthorName.Text = Books[id].Author;
                RTBi_BookBarcode.Text = Books[id].Barcode;

                RTBi_Issue_BTSID.ReadOnly = false;

                label15.Text = "Issue To[BTS ID]";
                label16.Text = "Issue To[Name]";
                label17.Text = "Issue To[Level]";

                B_Issue.Text = "Issue";

                RTBi_Issue_BTSID.Text = "";

                IssueContainer.Visible = true;
                IssueContainer.Enabled = true;
                issueUIReady = true;

            }
            else
            {
                AvailabilityStatus.Text = "Issued";
                AvailabilityStatus.ForeColor = Color.FromArgb(255, 31, 31);

                RTBi_BookName.Text = Books[id].Name;
                RTBi_AuthorName.Text = Books[id].Author;
                RTBi_BookBarcode.Text = Books[id].Barcode;

                RTBi_Issue_BTSID.ReadOnly = true;

                B_Issue.Text = "Return";

                label15.Text = "Issued To[BTS ID]";
                label16.Text = "Issued To[Name]";
                label17.Text = "Issued To[Level]";

                RTBi_Issue_Name.Text = Books[id].IssuedBy;
                RTBi_Issue_BTSID.Text = Books[id].IssuedBy_BTSCID;
                if (Books[id].IssuedBy_Level == "Student")
                {
                    RTBi_Issue_Level.Text = Books[id].IssuedBy_Level + " of " + Books[id].IssuedBy_Section;
                }
                else
                {
                    RTBi_Issue_Level.Text = Books[id].IssuedBy_Level;
                }

                IssueContainer.Visible = true;
                IssueContainer.Enabled = true;
                issueUIReady = true;
            }
        }

        void RemoveInfo()
        {
            IssueContainer.Visible = false;
            IssueContainer.Enabled = false;
            issueUIReady = false;
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
                    //await UpdateIssue(id, RTBi_Issue_Name.Text, RTBi_Issue_BTSID, !Books[id].Available);
                    string section = null;
                    string level = null;
                    foreach (User user in Users)
                    {
                        if (RTBi_Issue_BTSID.Text.Trim().ToLower() == user.BTSCID.ToLower())
                        {
                            section = user.Section;
                            level = user.Level;
                        }
                    }
                    await UpdateIssue(id, RTBi_Issue_Name.Text.Trim(), RTBi_Issue_BTSID.Text.Trim().ToUpper(), section, level, !Books[id].Available);
                }
                else
                {
                    //Return Book
                    await UpdateIssue(id, null, null, null, null, !Books[id].Available);
                }
                UseWaitCursor = false;
                RemoveInfo();
                MessageBox.Show("The Entry has been updated!", "Success", MessageBoxButtons.OK);
                RefreshBooks();
                RTBi_Issue_BTSID.Text = "";
                RTBi_Issue_Name.Text = "";
                RTBi_Issue_Level.Text = "";
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
                    RTBi_BookName.Text = Books[id].Name;
                    RTBi_AuthorName.Text = Books[id].Author;
                    RTBi_BookBarcode.Text = Books[id].Barcode;
                }
                else
                {
                    //Issued Book
                    RTBi_BookName.Text = Books[id].Name;
                    RTBi_AuthorName.Text = Books[id].Author;
                    RTBi_BookBarcode.Text = Books[id].Barcode;
                }
                return;
            }

            try
            {
                UseWaitCursor = true;
                if (Books[id].Available)
                {
                    //Available Book
                    await EditEntry(id, RTBi_BookName.Text.Trim(), RTBi_AuthorName.Text.Trim(), RTBi_BookBarcode.Text.Trim(), Books[id].RegistrationDate, null, null, null, null, null, Books[id].Available);
                }
                else
                {
                    //Issued Book
                    await EditEntry(id, RTBi_BookName.Text.Trim(), RTBi_AuthorName.Text.Trim(), RTBi_BookBarcode.Text.Trim(), Books[id].RegistrationDate, Books[id].IssueDate, Books[id].IssuedBy, Books[id].IssuedBy_BTSCID, Books[id].IssuedBy_Level, Books[id].IssuedBy_Section, Books[id].Available);
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

        public void GenerateDailyReport()
        {
            RTB_DR_IssuedBooks.Text = issuedBooks.ToString();
            RTB_DR_TotalBooks.Text = totalBooks.ToString();
            RTB_DR_OverdueBooks.Text = overdueBooks.ToString();
            RTB_DR_IssueTeachers.Text = issuedtoTeachers.ToString();
            RTB_DR_IssueStudents.Text = issuedtoStudents.ToString();
            RTB_DR_URD.Text = upcomingDeadlineBooks.ToString();
            label27.Text = "Date: " + DateTime.Today.ToString("d");

            LoadGridView_DailyReport();

            isDailyReportGenerated = true;
        }

        public void LoadGridView_DailyReport()
        {
            foreach (Book book in Overdue_Books)
            {
                string issueDate = "";
                if (book.IssueDate == "null")
                {
                    issueDate = "Not Issued";
                }
                else
                {
                    issueDate = book.IssueDate;
                }
                string dueDate = "";
                if (book.DueDate == "null")
                {
                    dueDate = "Not Issued";
                }
                else
                {
                    dueDate = book.DueDate;
                }
                string issuedto = "";
                if (book.IssuedBy == "null")
                {
                    issuedto = "Not Issued";
                }
                else
                {
                    issuedto = book.IssuedBy + "[" + book.IssuedBy_BTSCID + "]";
                }

                string status = "Overdue";

                GV_DR_Database.Rows.Add(book.ID, status, book.Name, book.Author, book.Barcode, book.Barcode_BTSCID, book.RegistrationDate, issueDate, dueDate, issuedto, book.Available);
            }
            foreach (Book book in UpcomingDeadline_Books)
            {
                string issueDate = "";
                if (book.IssueDate == "null")
                {
                    issueDate = "Not Issued";
                }
                else
                {
                    issueDate = book.IssueDate;
                }
                string dueDate = "";
                if (book.DueDate == "null")
                {
                    dueDate = "Not Issued";
                }
                else
                {
                    dueDate = book.DueDate;
                }
                string issuedto = "";
                if (book.IssuedBy == "null")
                {
                    issuedto = "Not Issued";
                }
                else
                {
                    issuedto = book.IssuedBy + "[" + book.IssuedBy_BTSCID + "]";
                }

                string status = "Upcoming Deadline";

                GV_DR_Database.Rows.Add(book.ID, status, book.Name, book.Author, book.Barcode, book.Barcode_BTSCID, book.RegistrationDate, issueDate, dueDate, issuedto, book.Available);
            }
        }

        public void ViewDailyReport()
        {
            PanelSwitcher(DailyReportPanel);
        }

        public void LoadSettings()
        {
            //Load Settings
            EnableAutoShowReports = Properties.Settings.Default.EnableAutoShowReports;
            EnableAutoSaveReports = Properties.Settings.Default.EnableAutoSaveReports;
            PreviousSaveDate = Properties.Settings.Default.PreviousSaveDate;

            //Set Correct Info
            CB_EnableAutoShowReports.Checked = EnableAutoShowReports;
            CB_EnableAutoSaveReports.Checked = EnableAutoSaveReports;
        }

        public void AutoSaveDailyReport() 
        {
            string folderpath = "";
            folderpath = CreateFolderInDocuments("Library Management System");
            exportToExcel.ReportType = "DailyReport";
            exportToExcel.SetData(totalBooks, issuedBooks, overdueBooks, issuedtoTeachers, issuedtoStudents, upcomingDeadlineBooks);
            exportToExcel.Export(GV_DR_Database, folderpath);
        }

        #endregion

        #region UICode
        public void ApplyFonts()
        {
            pfc = Font_Manager.pfc;

            //Tab Strip
            label1.Font = new Font(pfc.Families[0], 21);
            HomeButton.Font = new Font(pfc.Families[0], 12);
            LibraryButton.Font = new Font(pfc.Families[0], 12);
            ManageButton.Font = new Font(pfc.Families[0], 12);
            RegisterButton.Font = new Font(pfc.Families[0], 12);
            SettingsButton.Font = new Font(pfc.Families[0], 12);

            //Home Panel
            Home.Font = new Font(pfc.Families[0], 25);

            //Library Panel
            label4.Font = new Font(pfc.Families[0], 25);
            RTB_Search.Font = new Font(pfc.Families[0], 14);
            B_Search.Font = new Font(pfc.Families[0], 14);
            B_Refresh.Font = new Font(pfc.Families[0], 14);

            //Register Panel
            label2.Font = new Font(pfc.Families[0], 25);
            label10.Font = new Font(pfc.Families[0], 17);
            label11.Font = new Font(pfc.Families[0], 17);
            label12.Font = new Font(pfc.Families[0], 17);
            RTB_Name.Font = new Font(pfc.Families[0], 16);
            RTB_Author.Font = new Font(pfc.Families[0], 16);
            RTB_Barcode.Font = new Font(pfc.Families[0], 16);
            B_Register.Font = new Font(pfc.Families[0], 19);

            //Issue Panel
            label5.Font = new Font(pfc.Families[0], 25);
            B_Edit.Font = new Font(pfc.Families[0], 15);
            B_Delete.Font = new Font(pfc.Families[0], 15);
            B_Issue.Font = new Font(pfc.Families[0], 15);
            label17.Font = new Font(pfc.Families[0], 15);
            label16.Font = new Font(pfc.Families[0], 15);
            label15.Font = new Font(pfc.Families[0], 15);
            label14.Font = new Font(pfc.Families[0], 15);
            label13.Font = new Font(pfc.Families[0], 15);
            label9.Font = new Font(pfc.Families[0], 15);
            RTBi_BookName.Font = new Font(pfc.Families[0], 15);
            RTBi_AuthorName.Font = new Font(pfc.Families[0], 15);
            RTBi_BookBarcode.Font = new Font(pfc.Families[0], 15);
            RTBi_Issue_BTSID.Font = new Font(pfc.Families[0], 15);
            RTBi_Issue_Name.Font = new Font(pfc.Families[0], 15);
            RTBi_Issue_Level.Font = new Font(pfc.Families[0], 15);
            AvailabilityStatus.Font = new Font(pfc.Families[0], 16);
            RTB_BookID.Font = new Font(pfc.Families[0], 16);
            B_GetInfo.Font = new Font(pfc.Families[0], 15);
            label7.Font = new Font(pfc.Families[0], 17);

            //Settings Panel
            label3.Font = new Font(pfc.Families[0], 25);
            CB_EnableAutoShowReports.Font = new Font(pfc.Families[0], 17);
            CB_EnableAutoSaveReports.Font = new Font(pfc.Families[0], 17);
            label21.Font = new Font(pfc.Families[0], 17);

            //Loading Panel
            label6.Font = new Font(pfc.Families[0], 30);

            //Daily Report Panel
            L_DR_IssuedBooks.Font = new Font(pfc.Families[0], 17);
            label22.Font = new Font(pfc.Families[0], 17);
            label23.Font = new Font(pfc.Families[0], 17);
            label24.Font = new Font(pfc.Families[0], 17);
            label25.Font = new Font(pfc.Families[0], 14);
            label26.Font = new Font(pfc.Families[0], 15);
            B_DR_Back.Font = new Font(pfc.Families[0], 16);
            label8.Font = new Font(pfc.Families[0], 25);
            label27.Font = new Font(pfc.Families[0], 17);
            RTB_DR_IssuedBooks.Font = new Font(pfc.Families[0], 16);
            RTB_DR_TotalBooks.Font = new Font(pfc.Families[0], 16);
            RTB_DR_IssueTeachers.Font = new Font(pfc.Families[0], 16);
            RTB_DR_IssueStudents.Font = new Font(pfc.Families[0], 16);
            RTB_DR_OverdueBooks.Font = new Font(pfc.Families[0], 16);
            RTB_DR_URD.Font = new Font(pfc.Families[0], 16);
            B_SaveToExcel.Font = new Font(pfc.Families[0], 15);
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
            PanelSwitcher(HomePanel);
            UI_Manager.TabButtonClicked(HomeButton);
        }

        private void LibraryButton_Click(object sender, EventArgs e)
        {
            PanelSwitcher(LibraryPanel);
            GV_Database.PerformLayout();
            UI_Manager.TabButtonClicked(LibraryButton);
        }

        private void ManageButton_Click(object sender, EventArgs e)
        {
            PanelSwitcher(IssuePanel);
            UI_Manager.TabButtonClicked(ManageButton);
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            PanelSwitcher(RegisterPanel);
            UI_Manager.TabButtonClicked(RegisterButton);
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            PanelSwitcher(SettingsPanel);
            UI_Manager.TabButtonClicked(SettingsButton);
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

                Random random = new Random();
                int randomNumber = random.Next(0, 10000);
                string BTSID = "BTS-" + random.Next(0, 10000).ToString("D4") + "-" + random.Next(0, 100000).ToString("D5");

                foreach (Book book in Books)
                {
                    bool isFound = false;
                    while (isFound == false)
                    {
                        if (BTSID != book.ID)
                        {
                            isFound = true;
                        }
                        else
                        {
                            BTSID = "BTS-" + random.Next(0, 10000).ToString("D4") + "-" + random.Next(0, 100000).ToString("D5");
                        }
                    }
                }

                await AddBookAsync(bookData.Name, bookData.Author, bookData.Barcode, bookData.RegistrationDate, bookData.Available, BTSID);
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

        private void B_Refresh_Click(object sender, EventArgs e)
        {
            RefreshBooks();
        }

        private void RTB_Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Search();
            }
        }

        private void GV_Database_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == GV_Database.Columns[10].Index && e.RowIndex >= 0)
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
            if (e.KeyCode == Keys.Enter) 
            {
                Console.WriteLine("Enter Pressed!");        
                CheckInfoFromID(); 
            }
        }

        private void B_Issue_Click(object sender, EventArgs e)
        {
            //REIMPLEMENT

            //Check if empty
            if (String.IsNullOrWhiteSpace(RTBi_Issue_BTSID.Text))
            {
                MessageBox.Show("The 'BTS ID' cannot be left empty", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (RTBi_Issue_Name.Text.Trim() == "")
            {
                MessageBox.Show("BTSC ID not found", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Command to Update
            CmdUpdateIssue();
        }

        private void B_Edit_Click(object sender, EventArgs e)
        {
            //Check if empty
            if (String.IsNullOrWhiteSpace(RTBi_BookName.Text) || String.IsNullOrWhiteSpace(RTBi_AuthorName.Text) || String.IsNullOrWhiteSpace(RTBi_BookBarcode.Text))
            {
                MessageBox.Show("Any Fields cannot be left empty", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CmdEditEntry();
        }

        private void B_Delete_Click(object sender, EventArgs e)
        {
            int id = int.Parse(RTB_BookID.Text.Trim());
            CmdDeleteEntry(id);
        }

        private void RTBi_Issue_BTSID_TextChanged(object sender, EventArgs e)
        {
            if (AvailabilityStatus.Text == "Issued") { return; }
            RTBi_Issue_Level.Text = "";
            RTBi_Issue_Name.Text = "";

            // Format to BTS-xx-xxx
            string input = RTBi_Issue_BTSID.Text.Replace("BTS-", "").Replace("-", "");
            if (input.Length > 2)
                input = input.Insert(2, "-");

            RTBi_Issue_BTSID.TextChanged -= RTBi_Issue_BTSID_TextChanged;
            RTBi_Issue_BTSID.Text = "BTS-" + input;
            RTBi_Issue_BTSID.SelectionStart = RTBi_Issue_BTSID.Text.Length;
            RTBi_Issue_BTSID.TextChanged += RTBi_Issue_BTSID_TextChanged;

            // Put Text in other boxes
            if (RTBi_Issue_BTSID.Text.Length >= 8 && !String.IsNullOrEmpty(RTBi_Issue_BTSID.Text))
            {
                foreach (User user in Users)
                {
                    if (RTBi_Issue_BTSID.Text.Trim().ToLower() == user.BTSCID.Trim().ToLower())
                    {
                        RTBi_Issue_Name.Text = user.Name;
                        if (user.Level == "Teacher")
                        {
                            RTBi_Issue_Level.Text = user.Section + " Teacher";
                        }
                        else if (user.Level == "Student")
                        {
                            RTBi_Issue_Level.Text = "Student of " + user.Section;
                        }
                    }
                }
            }
        }
        private void B_ViewDailyReport_Click(object sender, EventArgs e)
        {
            ViewDailyReport();
        }

        private void B_DR_Back_Click(object sender, EventArgs e)
        {
            PanelSwitcher(HomePanel);
        }
        private void GV_DR_Database_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == GV_DR_Database.Columns[11].Index && e.RowIndex >= 0)
            {
                //View Button Clicked
                string value = GV_DR_Database.Rows[e.RowIndex].Cells[0].Value.ToString();
                ViewBook(value);
            }
        }
        private void B_SaveToExcel_Click(object sender, EventArgs e)
        {
            string folderpath = "";
            folderpath = CreateFolderInDocuments("Library Management System");
            exportToExcel.ReportType = "DailyReport";
            exportToExcel.SetData(totalBooks, issuedBooks, overdueBooks, issuedtoTeachers, issuedtoStudents, upcomingDeadlineBooks);
            exportToExcel.Export(GV_DR_Database, folderpath);
            //exportToExcel.ExportToCsv(GV_DR_Database, folderpath);
        }

        private void CB_EnableAutoShowReports_CheckedChanged(object sender, EventArgs e)
        {
            EnableAutoShowReports = CB_EnableAutoShowReports.Checked;
            Properties.Settings.Default.EnableAutoShowReports = EnableAutoShowReports;
            Properties.Settings.Default.Save();
        }

        private void CB_EnableAutoSaveReports_CheckedChanged(object sender, EventArgs e)
        {
            EnableAutoSaveReports = CB_EnableAutoSaveReports.Checked;
            Properties.Settings.Default.EnableAutoSaveReports = EnableAutoSaveReports;
            Properties.Settings.Default.Save();
        }
        #endregion

        public string CreateFolderInDocuments(string folderName)
        {
            try
            {
                //string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string documentsPath = @"D:\\Documents";

                string newFolderPath = Path.Combine(documentsPath, folderName);


                if (Directory.Exists(newFolderPath))
                {
                    // Open the folder in File Explorer
                    return newFolderPath;
                }
                else
                {
                    Directory.CreateDirectory(newFolderPath);
                    return newFolderPath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating folder: " + ex.Message);
                return null;
            }
        }
    }
}
