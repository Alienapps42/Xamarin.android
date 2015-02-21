using System;
using DropboxSync.Android;

namespace Xamarin_DropBox_DataStore
{
	public class Contacto
	{
		public string Id {get;set;}
		public string Nombre {get;set;}
		public string Telefono {get;set;}

		public Contacto (string Id,string Nombre,string Telefono)
		{
			this.Id = Id;
			this.Nombre = Nombre;
			this.Telefono = Telefono;
		}

		//REGRESA LAS PROPIEDADES DEL OBJECTO Y SUS VALORES EN UN VARIABLE TIPO DBFields
		public DBFields Campos ()
		{
			var fields = new DBFields();
			fields.Set("Id", Id);
			fields.Set("Nombre", Nombre);
			fields.Set("Telefono", Telefono);

			return fields;
		}

	}
}

