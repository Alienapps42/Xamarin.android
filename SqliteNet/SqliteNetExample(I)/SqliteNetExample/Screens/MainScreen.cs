/*
MainsScreen.cs
Actividad principal de la aplicación
Autor: Hugo Gómez Arenas
Email: hugo.gomeza@outlook.com
http://xamurais.com
*/


using System;
using System.Collections;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using SQLite;
using SqliteNetExample.Models;


namespace SqliteNetExample
{
	[Activity (Label = "SqliteNetExample", MainLauncher = true)]
	public class MainScreen : Activity
	{
		SQLiteConnection db;

		EditText txtName;
		EditText txtAddress;
		ListView lstCustomer;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
		
			SetContentView (Resource.Layout.Main);
			LoadConnection ();

			Button btnAddCustomer = FindViewById<Button> (Resource.Id.btnAddCustomer);
			txtName = FindViewById<EditText> (Resource.Id.txtName);
			txtAddress = FindViewById<EditText> (Resource.Id.txtAddress);
			lstCustomer = FindViewById<ListView> (Resource.Id.lstCustomers);


			LoadList ();

			btnAddCustomer.Click+= (sender, e) => {
				Customer customer = new Customer {Name = txtName.Text , Address = txtAddress.Text};

				//Registra en la base de datos
				db.Insert(customer);

				LoadList();
			};


		}

		//Crea la conexión con la base de datos
		private void LoadConnection()
		{
			string folder = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal);
			string dbPath = System.IO.Path.Combine (folder, "bussines2.db");
			db = new SQLiteConnection (dbPath);
		}

		//Para mostrar en la lista los registros ya almacenados
		private void LoadList()
		{

			var query= db.Table<Customer> ();

			ArrayList lst = new ArrayList ();
			foreach (var customer in query) {
				lst.Add (customer.Name);
			}

			string[] arr = (String[]) lst.ToArray(typeof(string));
			ArrayAdapter<String> adapter =
				new ArrayAdapter<String> (this, Android.Resource.Layout.SimpleListItem1,arr);
					
			lstCustomer.Adapter = adapter;
		}



	}

	
}


