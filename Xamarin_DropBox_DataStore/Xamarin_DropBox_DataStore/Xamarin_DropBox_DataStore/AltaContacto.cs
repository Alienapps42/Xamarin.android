using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Util;
using DropboxSync.Android;

namespace Xamarin_DropBox_DataStore
{
	[Activity (Label = "Xamarin_DropBox_DataStore",  Icon = "@drawable/icon")]
	public class AltaContacto : Activity
	{

		Button btnGuardar;
		EditText txtNombre;
		EditText txtTelefono;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.alta_contacto);

			btnGuardar= FindViewById<Button>(Resource.Id.btnGuardar);
			txtNombre = FindViewById<EditText> (Resource.Id.txtNombre);
			txtTelefono = FindViewById<EditText> (Resource.Id.txtTelefono);

			btnGuardar.Click += delegate {

				//VERIFICAMO QUE TENGA UN NOMBRE Y TELEFONO
				if(txtNombre.Text.Equals ("") ||txtTelefono.Text.Equals(""))
					Toast.MakeText (this,"Falto colocar algún dato", ToastLength.Long).Show();
				else
					RegistraContacto(txtNombre.Text,txtTelefono.Text);

			};


			IniciaConexionCuentaDropBox ();
		}

		//REGISTRA CONTACTO
		void RegistraContacto (String Nombre,String Telefono)
		{

			//INICIALIZAMOS NUESTRO DATASTORE
			InicializarDropboxDatastore (Account.LinkedAccount);

			//RECUPERAMOS LA TABLA DE NUESTRO DATASTORE PARA REGISTRAR EN ELLA EL NUEVO CONTACTO
			DBTable  table = DropboxDatastore.GetTable ("Contactos");

			int New_id = 1;
			//RECUPERAMOS TODO LOS CONTACTOS DE LA TABLA
			var results = table.Query ();

			//RECUPERAMOS EL ULTIMO ID DEL CONTACTO REGISTRADO Y LO AUMENTAMOS PARA REGISTRA OTRO NUEVO Y NO SE
			//DUPLIQUEN
			if (results.Count () > 0) {

				DBRecord UltimoRegistro=results.AsList () [results.Count()-1];

				New_id = int.Parse (UltimoRegistro.GetString ("Id")) + 1;
			}


			//CREAMOS UN OBJECTO DE TIPO CONTACTO
			Contacto cto = new Contacto (New_id.ToString(),Nombre,Telefono);


			//REGISTRAMOS EL OBJECTO CONTACTO PASANDO LA PROPIEDAD DE "Campos" QUE ES DE TIPO DBFields
			table.Insert(cto.Campos());

			//VERIFICAMOS QUE NUESTRA DATASTORE ESTE ABIERTA PARA PODER REALIZAR EL ALTA DEL CONACTO
			if (!EstatusDataStore ()) {
				//REINICIAMOS LA CONEXION CON DROPBOX
				ReiniciarConexion (); 
			} else {


				// SI NO HAY NINGUN PROBLEMA DE CONEXION PROCEDEMOS A REALIZAR LA SINCRONIZACION CON NUESTRO DATASTORE
				// Y DAR DE ALTA NUESTRO CONTACTO
				DropboxDatastore.Sync ();

				Toast.MakeText (this,"Contacto Registrado", ToastLength.Long).Show();

				txtNombre.Text = "";
				txtTelefono.Text= "";

				//CERRAMOS LA CONEXION A NUESTRO DATASTORE
				DropboxDatastore.Close ();


			}



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
}


