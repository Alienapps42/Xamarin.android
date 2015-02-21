
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
	[Activity (Label = "EditarContacto")]			
	public class EditarContacto : Activity
	{

		//VARIABLE TEMPORAL QUE CONTENDRA EL CONTACTO 
		DBRecord temp_contacto;

		Button btnActualizar;
		Button btnBorrar;
		EditText txtNombre;
		EditText txtTelefono;

		//ID EL CONTACTO A BUSCAR
		string id;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Create your application here
			SetContentView (Resource.Layout.editar_contacto);

			btnActualizar = FindViewById<Button> (Resource.Id.btnActualizar);
			btnBorrar = FindViewById<Button> (Resource.Id.btnBorrar);
			txtNombre = FindViewById<EditText> (Resource.Id.txtNombre);
			txtTelefono = FindViewById<EditText> (Resource.Id.txtTelefono);


			//ACTUALIZA LOS DATOS DEL CONTACTO
			btnActualizar.Click += delegate {

				//VERIFICAMOS QUE LA VARAIBLE NO SEA NULL
				if(temp_contacto !=null)
				{
					//INICIALIZAMOS EL DATASTORE
					InicializarDropboxDatastore (Account.LinkedAccount);

					//ASIGNAMOS LOS NUEVOS VALORES
					temp_contacto.Set ("Nombre", txtNombre.Text);
					temp_contacto.Set ("Telefono", txtTelefono.Text );


					//SINCRONIZAMOS PARA CONFIRMAR LOS CAMBIOS
					DropboxDatastore.Sync ();
					Toast.MakeText (this, "Se actualizo el contacto", ToastLength.Long).Show();

					//CERRAMOS EL DATASTORE
					DropboxDatastore.Close();
				}else{

					Toast.MakeText (this, "No hay objecto para actualizar", ToastLength.Long).Show();
				}


			};

			//BORRA EL CONTACTO
			btnBorrar.Click += delegate {

				//VERIFICAMOS QUE LA VARAIBLE NO SEA NULL
				if(temp_contacto !=null)
				{
					//INICIALIZAMOS EL DATASTORE
					InicializarDropboxDatastore (Account.LinkedAccount);

					//INVOCAMOS EL METODO DELETE 
					temp_contacto.DeleteRecord ();

					//SINCRONIZAMOS PARA CONFIRMAR LOS CAMBIOS
					DropboxDatastore.Sync ();

					Toast.MakeText (this,"Contacto Borrado", ToastLength.Long).Show();
					txtNombre.Text = "";
					txtTelefono.Text= "";

					//CERRAMOS EL DATASTORE
					DropboxDatastore.Close();
				}else{

					Toast.MakeText (this, "No hay objecto para eliminar", ToastLength.Long).Show();
				}


			};



			id = Intent.GetStringExtra ("id") ?? "0";

			IniciaConexionCuentaDropBox ();

		}


		//REGISTRA CONTACTO
		void BuscaContacto (string id)
		{
			InicializarDropboxDatastore (Account.LinkedAccount);

			//RECUPERAMOS LA TABLA DE NUESTRO DATASTORE PARA REGISTRAR EN ELLA EL NUEVO CONTACTO
			DBTable  table = DropboxDatastore.GetTable ("Contactos");


			//RECUPERA CONTACTO CREANDO UN FILTRO 
			var fieldsFiltro = new DBFields();
			fieldsFiltro.Set("Id", id);

			//APLICAMOS EL FILTRO AL QUERY
			var results = table.Query (fieldsFiltro);

			//VERIFICAMOS EL RESULTADO Y SI TIENE RECUPERAMOS EL CONTACTO
			if (results.Count () > 0) {
				temp_contacto = results.AsList () [0];

				txtNombre.Text = temp_contacto.GetString ("Nombre");
				txtTelefono.Text = temp_contacto.GetString ("Telefono");

			} else {
				Toast.MakeText (this, "No se encontro el contacto", ToastLength.Long).Show();
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
			}else {
				//REALIZAMOS LA BUSQUEDA DEL CONTACTO SELECCIONADO
				BuscaContacto (id);
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

