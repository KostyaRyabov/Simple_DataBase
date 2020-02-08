using System;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Data.SqlClient;

namespace lab
{
    public partial class MainWindow : Window
    {
        private string connectionString = @"Data Source=DESKTOP-P65HMEH\SQLEXPRESS;Initial Catalog=lab;Integrated Security=True";
        Dictionary<string, Dictionary<int, string>> dic = new Dictionary<string, Dictionary<int, string>>();
        private string curTable = "";
        private int StoredSize = 0;

        List<dynamic> obj_list = new List<dynamic>();

        private void Insert(string TABLE)
        {
            if (TABLE != "")
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string cmd = "";
                    bool hasRows = false;
                    int startID = -1;

                    foreach (var item in myDataGrid.Items)
                    {
                        var row = myDataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;

                        if (row.Background == Brushes.Orange)
                        {
                            if (startID == -1)
                            {
                                startID = row.GetIndex()-1;
                            }

                            hasRows = true;
                            cmd += $"INSERT INTO {TABLE} VALUES (";

                            for (int i = 1; i < myDataGrid.Columns.Count; i++)
                            {
                                if (i > 1) cmd += ", ";
                                string param = myDataGrid.Columns[i].Header.ToString();

                                try
                                {
                                    if (((IDictionary<string, Object>)item)[param].GetType() == typeof(string)) cmd += $"'{((IDictionary<string, Object>)item)[param]}'";
                                    else if (((IDictionary<string, Object>)item)[param].GetType() == typeof(DBNull)) cmd += "null";
                                    else cmd += ((IDictionary<string, Object>)item)[param].ToString();
                                }
                                catch
                                {
                                    cmd += "null";
                                }
                            }

                            cmd += ") ";
                            row.Background = Brushes.White;
                        }
                    }

                    if (hasRows)
                    {
                        SqlCommand command = new SqlCommand(cmd, connection);
                        command.ExecuteNonQuery();

                        if (startID > 0) startID = (int)((IDictionary<string, Object>)obj_list[startID])["id"];

                        command = new SqlCommand($"SELECT * FROM {TABLE} WHERE id > {startID}", connection);
                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {   
                            ((IDictionary<string, Object>)obj_list[StoredSize++])["id"] = reader[0];
                        }
                    }
                }
            }
        }
        private void Update(string TABLE)
        {
            if (TABLE != "")
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string cmd = "";
                    bool hasRows = false;

                    foreach (var item in myDataGrid.Items)
                    {
                        var row = myDataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;

                        if (row.Background == Brushes.Yellow)
                        {
                            hasRows = true;
                            cmd += $"UPDATE {TABLE} SET ";

                            for (int i = 1; i < myDataGrid.Columns.Count; i++)
                            {
                                if (i > 1) cmd += ", ";
                                string param = myDataGrid.Columns[i].Header.ToString();

                                try
                                {
                                    if (((IDictionary<string, Object>)item)[param].GetType() == typeof(string)) cmd += $"{param}='{((IDictionary<string, Object>)item)[param]}'";
                                    else if (((IDictionary<string, Object>)item)[param].GetType() == typeof(DBNull)) cmd += $"{param}=null";
                                    else cmd += $"{param}={((IDictionary<string, Object>)item)[param]}";
                                }
                                catch
                                {
                                    cmd += $"{param}=null";
                                }
                            }

                            cmd += $" WHERE id={((IDictionary<string, Object>)item)["id"]} ";
                            row.Background = Brushes.White;
                        }
                    }

                    if (hasRows)
                    {
                        SqlCommand command = new SqlCommand(cmd, connection);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
        private void Delete(string TABLE)
        {
            if (TABLE != "")
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string cmd = "";
                    bool hasRows = false;
                    int oldSize = obj_list.Count;

                    foreach (var item in myDataGrid.SelectedItems)
                    {
                        var row = myDataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                        
                        if (row.GetIndex() < oldSize)
                        {
                            if (row.Background != Brushes.Orange)
                            {
                                hasRows = true;
                                StoredSize--;
                                cmd += $"DELETE {TABLE} WHERE id={((IDictionary<string, Object>)item)["id"]} ";
                            }

                            obj_list.Remove(item);
                        }
                    }

                    if (hasRows)
                    {
                        SqlCommand command = new SqlCommand(cmd, connection);
                        command.ExecuteNonQuery();
                    }

                    myDataGrid.Items.Refresh();
                }
            }
        }
        private void Load(string TABLE, string args = "")
        {
            if (TABLE != "") {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand($"SELECT * FROM {TABLE} {args}", connection))
                    {
                        if (curTable != "") (FindName(curTable + "Bn") as Button).IsEnabled = true;
                        curTable = TABLE;
                        (FindName(TABLE + "Bn") as Button).IsEnabled = false;

                        insertBn.IsEnabled = false;
                        updateBn.IsEnabled = false;
                        deleteBn.IsEnabled = false;
                        canselBn.IsEnabled = false;
                        
                        SqlDataReader reader = command.ExecuteReader();

                        dic.Clear();
                        myDataGrid.ItemsSource = null;
                        myDataGrid.Items.Clear();
                        myDataGrid.Columns.Clear();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.GetName(i).EndsWith("ID"))           // example: contractID  -> table: contract + pointer: ID
                            {
                                string tableName = reader.GetName(i).Substring(0, reader.GetName(i).Length - 2);
                                LoadDictionary(tableName);

                                DataGridComboBoxColumn comboBoxColumn = new DataGridComboBoxColumn();
                                comboBoxColumn.Header = reader.GetName(i);
                                comboBoxColumn.ItemsSource = dic[tableName];
                                comboBoxColumn.DisplayMemberPath = "Value";
                                comboBoxColumn.SelectedValuePath = "Key";
                                comboBoxColumn.SelectedValueBinding = new Binding(reader.GetName(i));
                                myDataGrid.Columns.Add(comboBoxColumn);
                            }
                            else if (reader.GetName(i) == "date")
                            {
                                DataGridTemplateColumn col = new DataGridTemplateColumn();
                                FrameworkElementFactory datePickerFactoryElem = new FrameworkElementFactory(typeof(DatePicker));
                                Binding dateBind = new Binding(reader.GetName(i));
                                dateBind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                                dateBind.Mode = BindingMode.TwoWay;
                                datePickerFactoryElem.SetValue(DatePicker.SelectedDateProperty, dateBind);
                                datePickerFactoryElem.SetValue(DatePicker.DisplayDateProperty, dateBind);
                                DataTemplate cellTemplate = new DataTemplate();
                                cellTemplate.VisualTree = datePickerFactoryElem;
                                col.CellTemplate = cellTemplate;
                                col.Header = "date";
                                myDataGrid.Columns.Add(col);
                            }
                            else
                            {
                                DataGridTextColumn textColumn = new DataGridTextColumn();
                                textColumn.Header = reader.GetName(i);
                                textColumn.Binding = new Binding(reader.GetName(i));
                                if (i == 0) textColumn.IsReadOnly = true;
                                myDataGrid.Columns.Add(textColumn);
                            }
                        }
                        
                        obj_list.Clear();

                        while (reader.Read())
                        {
                            dynamic d_obj = new System.Dynamic.ExpandoObject();
                            for (int i = 0; i < reader.FieldCount; i++) ((IDictionary<string, Object>)d_obj).Add(reader.GetName(i), reader[i]);
                            obj_list.Add(d_obj);
                        }

                        myDataGrid.ItemsSource = obj_list;
                        StoredSize = obj_list.Count;

                        if (TABLE == "employee")
                        {
                            LoadDictionary("employee");
                            cb1.ItemsSource = dic["employee"];
                            cb2.ItemsSource = dic["department"];
                            filter.IsEnabled = true;
                        }
                        else filter.IsEnabled = false;
                    }
                }
            }
        }
        private void LoadDictionary(string TABLE)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand($"SELECT * FROM {TABLE}", connection))
                {
                    SqlDataReader reader = command.ExecuteReader();
                    dic.Add(TABLE, new Dictionary<int, string>());
                    try
                    {
                        while (reader.Read()) dic[TABLE].Add(reader.GetInt32(0), reader.GetString(1));
                    }
                    catch { }
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Content)
            {
                case "сотрудники":
                    Load("employee");
                    break;
                case "отделы":
                    Load("department");
                    break;
                case "должности":
                    Load("post");
                    break;
                case "типы договоров":
                    Load("contract_type");
                    break;
                case "трудовой договор":
                    Load("contract");
                    break;
                case "вакансии":
                    Load("vacancy");
                    break;
                case "insert":
                    Insert(curTable);
                    break;
                case "update":
                    Update(curTable);
                    break;
                case "delete":
                    Delete(curTable);
                    break;
                case "x":
                    string request = "";

                    switch ((sender as Button).Name)
                    {
                        case "clear_cb1":
                            cb1.SelectedIndex = -1;
                            try
                            {
                                if (cb2.Text.Length > 0) request += $" departmentID={((KeyValuePair<int, string>)cb2.SelectedItem).Key}";
                            }
                            catch { };
                            break;
                        case "clear_cb2":
                            cb2.SelectedIndex = -1;
                            try
                            {
                                if (cb1.Text.Length > 0) request += $" FIO='{((KeyValuePair<int, string>)cb1.SelectedItem).Value}'";
                            }
                            catch { };
                            break;
                    }

                    if (request.Length > 0) request = " WHERE" + request;
                    Load("employee", request);
                    break;
                case "cansel":
                    Load(curTable);   //clear all unsaved editions
                    break;
            }
        }
        private void myDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Background == Brushes.White)
            {
                if (e.Row.GetIndex() < StoredSize)
                {
                    e.Row.Background = Brushes.Yellow;
                    updateBn.IsEnabled = true;
                }
                else
                {
                    e.Row.Background = Brushes.Orange;
                    insertBn.IsEnabled = true;
                }

                canselBn.IsEnabled = true;
            }
        }

        private void myDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            deleteBn.IsEnabled = (myDataGrid.Items.IndexOf(myDataGrid.SelectedItem) <= obj_list.Count - 1);
        }

        private void cb1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string request = "";

            switch ((sender as ComboBox).Name)
            {
                case "cb1":
                    try { 
                        request += $" FIO='{((KeyValuePair<int, string>)e.AddedItems[0]).Value}'";
                        if (cb2.Text.Length > 0) request += $" AND departmentID={((KeyValuePair<int, string>)cb2.SelectedItem).Key}";
                    }
                    catch { }
                    break;
                case "cb2":
                    try {
                        request += $" departmentID={((KeyValuePair<int, string>)e.AddedItems[0]).Key}";
                        if (cb1.Text.Length > 0) request += $" AND FIO='{((KeyValuePair<int, string>)cb1.SelectedItem).Value}'";
                    }
                    catch { }
                    break;
            }

            if (request.Length > 0) request = " WHERE" + request;

            Load("employee", request);
        }
    }
}