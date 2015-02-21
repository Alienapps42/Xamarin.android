
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DropboxSync.Android;
using Android.Util;

namespace Xamarin_DropBox_DataStore
{
	[Activity (Label = "ListaContactos",MainLauncher = true)]			
	public class ListaContactos : Activity
	{


		List<Contacto> Lista_Contactos;
		ListView lvContactos;
		adapter_listview lvadapter;

		Button btnAlta;
		Button btnActualizar;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.lista_contactos);


			//INICIALIZAMOS NUESTRA LISTA DE CONTACTOS 
			Lista_Contactos = new List<Contacto> ();
			//INICIALIZAMOS NUESTRO LISTVIEW
			lvContactos = FindViewById<ListView> (Resource.Id.lvContactos);
			//CONFIGURAMOS NUESTRO ADAPTER Y LE ASIGNAMOS LA LISTA DE CONTACTOS
			lvadapter=new adapter_listview(this, Lista_Contactos);
			//Y LE PASAMOS EL ADAPTER A NUESTRO LISTVIEW
			lvContactos.Adapter = lvadapter;

			//ASIGNAMOS EL EVENTO ITEMCLICK A NUESTRO LISTVIEW
			lvContactos.ItemClick += OnListItemClick;

			btnActualizar= FindViewById<Button> (Resource.Id.btnActualizar);
			btnActualizar.Click += delegate {
				//CARGAMOS LOS CONTACTOS
				RegresaContactos ();
			};

			btnAlta= FindViewById<Button> (Resource.Id.btnAlta);
			btnAlta.Click += delegate {
				var alta = new Intent (this, typeof(AltaContacto));
				StartActivity (alta);  

			};


			//INICIA LA CONEXION A DROPBOX
			IniciaConexionCuentaDropBox ();


		}

		protected void OnListItemClick(object sender, Android.Widget.AdapterView.ItemClickEventArgs e)
		{
			//RECUPERAMOS EL CONTACTO DEL ADAPTER
			var contacto = lvadapter[e.Position];

			//CREAMOS INTENT PARA PASAR EL ID DEL CONTACTO SELECCIONADO A LA ACTIVIDAD DE EDITAR
			var id = new Intent (this, typeof(EditarContacto));

			id.PutExtra ("id", contacto.Id);
			StartActivity (id); 

		}

		//REGRESA LOS CONTACTOS REGISTRADOS
		void RegresaContactos ()
		{
			//INICIALIZAMOS NUESTRO DATASTORE
			InicializarDropboxDatastore (Account.LinkedAccount);

			//RECUPERAMOS LA TABLA DE NUESTRO DATASTORE 
			DBTable  table = DropboxDatastore.GetTable ("Contactos");

			//INICIALIZAMOS LA LISTA DE CONTACTOS
			Lista_Contactos = new List<Contacto> ();


			//RECUPERAMOS TODOS LOS CONTACTOS DE NUESTRA TABLA
			var results = table.Query ();

			//SI HAY CONTACTOS LLENAMOS NUESTRA LISTA Y LA ASIGNAMOS A NUESTRO ADAPTER
			if (results.Count () > 0) {

				foreach (DBRecord row in results.AsList ()) {
					Lista_Contactos.Add (new Contacto (row.GetString ("Id"),row.GetString ("Nombre"),row.GetString ("Telefono")));
				}

				lvadapter.refresh (Lista_Contactos);
			}

			//SINCRONIZAMOS LOS DATOS
			DropboxDatastore.Sync ();

			//CERRAMOS LA CONEXION A NUESTRO DATASTORE
			DropboxDatastore.Close ();

		}






		//***********************************************************************************************************************************
		//***********************************************    CODIGO DROP BOX    *************************************************************
		//***********************************************************************************************************************************

		string DropboxSyncKey = "2w9i0dhge5aa8mv";
		string DropboxSyncSecret = "5q2dahwzyichd3h";

		DBAccountManager Account ; //VARIABLE DE LA CUENTA DE DROPBOX
		DBDatastore DropboxDatastore;//VARAIBLE DE ADMINISTRACION DE NUESTRA DATASTORE



		//INICIA LA CONEXION CON NUESTRA CUENTA DE DROPBOX
		void IniciaConexionCuentaDropBox ()
		{
			// INICIALIZAMOS NUESTRA VARIABLE ACCOUNT PASANDOLE LAS LLAVES QUE SE CREARON AL MOMENTO DE CREAR NUESTRA
			// APP EN EL PANEL DE DROPBOX CONSOLE
			Account = DBAccountManager.GetInstance (ApplicationContext, DropboxSyncKey, DropboxSyncSecret);

			//VERIFICAMOS SI LA CUENTA ESTA EN LINEA
			if (!Account.HasLinkedAccount) {
				//SI NO ESTA EN LINEA LLAMAMOS LA ACTIVIDAD DE INICIO DE SESION DE DROPBOX
				Account.StartLink (this, (int)RequestCode.LinkToDropboxRequest);
			}
		}

		//INICIALIZAMOS NUESTRO DATASTORE Y LE AGREGAMOS UN HADLE PARA IR VIENDO SI DESEAMOS EL ESTADO DE ESTA
		void InicializarDropboxDatastore (DBAccount account)
		{
			LogInfo("Inicializar DropboxDatastore");
			if (DropboxDatastore == null || !DropboxDatastore.IsOpen || DropboxDatastore.Manager.IsShutDown) {
				//INICIALIAZAMOS DropboxDatastore 
				DropboxDatastore = DBDatastore.OpenDefault (account ?? Account.LinkedAccount);
				DropboxDatastore.DatastoreChanged += HandleStoreChange;
			}
			//INVOCAMOS LA SINCRONIZACION DE DATASTORE
			DropboxDatastore.Sync();
		}

		//HADLE DEL DATASTORE
		void HandleStoreChange (object sender, DBDatastore.SyncStatusEventArgs e)
		{

			if (e.P0.SyncStatus.HasIncoming)
			{
				if (!Account.HasLinkedAccount) {
					LogInfo( "La conexion ha sido abandonada");
					DropboxDatastore.DatastoreChanged -= HandleStoreChange;
				}
				Console.WriteLine ("Datastore necesita sincronizarse.");
				DropboxDatastore.Sync ();

			}

		}


		//COMO ESTAMOS USANDO LA ACTIVIDAD DE INICIO DE SESION DE DROPBOX, ES NECESARIO CREAR  UN OnActivityResult, 
		//PARA PODER PROCESAR LA INFORMACION SOBRE LA VALIDACION DE LAS CREDENCIALES
		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			var code = (RequestCode)requestCode;
			//VERIFICAMOS QUE SU CUENTA ESTE AUTORIZADA
			if (code == RequestCode.LinkToDropboxRequest && resultCode != Result.Canceled) {
				//CUENTA VALIDA
				InicializarDropboxDatastore(Account.LinkedAccount);

			} else {
				Account.Unlink();
				IniciaConexionCuentaDropBox ();
			}
		}


		//VERIFICA EL ESTADO DE NUESTRA DATASTORE
		bool EstatusDataStore ()
		{
			if (!DropboxDatastore.IsOpen) {
				LogInfo ("Datastore no esta abierta.");
				return false;
			}
			if (DropboxDatastore.Manager.IsShutDown) {
				LogInfo ( "El administrador no esta activo.");
				return false;
			}
			if (!Account.HasLinkedAccount) {
				LogInfo ( "La cuenta no se encuentra en linea");
				return false;
			}
			return true;
		}


		//VERIFICA LA CONEXION DE LA CUENTA 
		void ReiniciarConexion ()
		{
			//SI LA CUENTA ESTA EN LINEA LA CIERRA
			if (Account.HasLinkedAccount) {
				Account.Unlink ();
			} else {
				//LLAMA A LA ACTIVIDAD DE INICIO DE SESION DE DROPBOX
				Account.StartLink (this, (int)RequestCode.LinkToDropboxRequest);
			}
		}


		//REGISTRA MENSAJE DE INFORMACION EN LOG DE XAMARIN
		void LogInfo(String mensaje)
		{
			Log.Info ("XamarinDropBox", mensaje);
		}
		//***********************************************************************************************************************************
		//***********************************************   FIN CODIGO DROP BOX   ***********************************************************
		//***********************************************************************************************************************************


	}

	/// ------------------------------------------------------------------------------------------------------------------
	/// -----------------------------------ADAPTADOR DEL LISTVIEW DE CONTACTOS--------------------------------------------
	/// ------------------------------------------------------------------------------------------------------------------
	public class adapter_listview: BaseAdapter<Contacto>
	{
		List<Contacto> items;
		Activity context;

		public adapter_listview(Activity context, List<Contacto> items)
			: base()
		{
			this.context = context;
			this.items = items;
		}

		public override Contacto this[int position]
		{
			get { return items[position]; }
		}

		public override long GetItemId(int position)
		{
			return position;
		}

		public override int Count
		{
			get { return items.Count; }
		}

		public void refresh(List<Contacto> items)
		{
			this.items = items;
			this.NotifyDataSetChanged ();
		} 

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			var item = items[position];
			View view = convertView;
			if (view == null) 
				view = context.LayoutInflater.Inflate(Resource.Layout.item_contacto, null);


			view.FindViewById<TextView> (Resource.Id.txtNombre).Text = item.Nombre;
			view.FindViewById<TextView> (Resource.Id.txtTelefono).Text = item.Telefono;

			return view;
		}

	}

	/// ------------------------------------------------------------------------------------------------------------------
	/// ------------------------------ FIN ADAPTADOR DEL LISTVIEW DE CONTACTOS--------------------------------------------
	/// ------------------------------------------------------------------------------------------------------------------


}

