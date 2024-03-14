using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RWDE_UPLOADS
{
    public partial class Form1 : Form
    {
        private readonly string connectionString = "Data Source=SOFTSELL\\MSSQLSERVER01;Initial Catalog=RWDE;Integrated Security=True";

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Select folder containing CSV files
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    folderBrowserDialog.Description = "Select folder containing CSV files";

                    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                    {
                        string folderPath = folderBrowserDialog.SelectedPath;
                        textBox1.Text = folderPath; // Set the folder path to textBox1
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string folderPath = textBox1.Text;

                if (!string.IsNullOrEmpty(folderPath))
                {
                    string[] csvFiles = Directory.GetFiles(folderPath, "*.csv");

                    int totalRowsInserted = 0;
                    int totalColumnsInserted = 0;
                    DateTime startTime = DateTime.Now;

                    foreach (string csvFilePath in csvFiles)
                    {
                        var result = InsertCsvDataIntoTable(csvFilePath);
                        totalRowsInserted += result.Item1;
                        totalColumnsInserted += result.Item2;
                    }

                    TimeSpan elapsedTime = DateTime.Now - startTime;

                    MessageBox.Show($"CSV data inserted into the database successfully.\n\n" +
                                    $"Time taken: {elapsedTime.TotalSeconds} seconds\n" +
                                    $"Total rows inserted: {totalRowsInserted}\n" +
                                    $"Total columns inserted: {totalColumnsInserted}");
                }
                else
                {
                    MessageBox.Show("Please select a folder containing CSV files.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private Tuple<int, int> InsertCsvDataIntoTable(string csvFilePath)
        {
            int rowsInserted = 0;
            int columnsInserted = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (StreamReader reader = new StreamReader(csvFilePath))
                    {
                        string headerLine = reader.ReadLine(); // Read and skip the header line

                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();

                            // Skip empty lines
                            if (string.IsNullOrEmpty(line))
                                continue;

                            string[] data = line.Split(',');

                            string ariesID = GetAriesID(data); // Get the ARIES ID from the data

                            // Check if ARIES ID is not null and not duplicate
                            if (!string.IsNullOrEmpty(ariesID))
                            {
                                if (IsDuplicateAriesID(ariesID, connection))
                                {
                                    MessageBox.Show($"Duplicate ARIES ID '{ariesID}' found. This record will not be inserted into the database.", "Duplicate ARIES ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    continue; // Skip to the next record
                                }

                                InsertClientData(connection, data);

                                rowsInserted++;
                                columnsInserted += data.Length;
                            }
                        }
                    }
                }

                return new Tuple<int, int>(rowsInserted, columnsInserted);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting CSV data into the table: {ex.Message}");
            }
        }

        private string GetAriesID(string[] data)
        {
            // Assuming ARIES ID is at index 9
            if (data.Length > 9)
                return data[9];
            else
                return null;
        }

        private bool IsDuplicateAriesID(string ariesID, SqlConnection connection)
        {
            string query = "SELECT COUNT(*) FROM Clients2 WHERE [ARIES ID] = @AriesID";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@AriesID", ariesID);
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        private void InsertClientData(SqlConnection connection, string[] data)
        {
            using (SqlCommand command = new SqlCommand("AddClientParameters", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Command", "Clients2");
                command.Parameters.AddWithValue("@FirstName", GetStringValue(data, 0));
                command.Parameters.AddWithValue("@LastName", GetStringValue(data, 1));
                command.Parameters.AddWithValue("@MiddleInitial", GetStringValue(data, 2));
                command.Parameters.AddWithValue("@MothersMaidenName", GetStringValue(data, 3));
                command.Parameters.AddWithValue("@DateOfBirth", GetStringValue(data, 4));
                command.Parameters.AddWithValue("@Gender", GetStringValue(data, 5));
                command.Parameters.AddWithValue("@IsRelatedOrAffected", GetStringValue(data, 6));
                command.Parameters.AddWithValue("@RecordIsShared", GetStringValue(data, 7));
                command.Parameters.AddWithValue("@URNExtended", GetStringValue(data, 8));
                command.Parameters.AddWithValue("@AriesID", GetStringValue(data, 9));
                command.Parameters.AddWithValue("@AgencyClientID", GetStringValue(data, 10));

                command.ExecuteNonQuery();
            }
        }

        private string GetStringValue(string[] data, int index)
        {
            if (index < data.Length)
                return data[index];
            else
                return string.Empty;
        }
    }
}
