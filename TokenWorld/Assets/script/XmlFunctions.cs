
using System.IO;
using System.Xml.Serialization;

/// <summary>
/// Xml関数関連クラス
/// </summary>
public static class XmlFunctions
{
	/// <summary>
	/// Xml読み込みメソッド
	/// </summary>
	/// <param name="strm">書き込みを行うストリーム</param>
	/// <returns>成功の場合：オブジェクト型　失敗の場合：null</returns>
	public static T XmlDeserialize<T>( Stream strm )
	{
		try {
			// XMLシリアルクラス作成
			XmlSerializer serializer = new XmlSerializer( typeof( T ) );
			// 逆シリアル化によりクラスに適用
			return (T)serializer.Deserialize( strm );
		} catch {
			return default( T );
		}
	}

	/// <summary>
	/// Xml書き込みメソッド
	/// </summary>
	/// <param name="strm">書き込みを行うストリーム</param>
	/// <param name="obj">書き込むデータ</param>
	public static bool XmlSerialize<T>( Stream strm, T obj )
	{
		// シリアライザ作成
		XmlSerializer serializer = new XmlSerializer( typeof( T ) );
		// シリアライズ
		serializer.Serialize( strm, obj );

		return true;
	}

	/// <summary>
	/// Xml読み込みメソッド
	/// </summary>
	/// <param name="path">読み込みを行うXmlのパス</param>
	/// <returns>成功の場合：オブジェクト型　失敗の場合：null</returns>
	public static T XmlDeserialize<T>( string path )
	{
		try
		{
			// 設定ファイルが存在するかチェック
			if( !System.IO.File.Exists( path ) ){
				return default(T);
			}
			// XMLシリアルクラス作成
			XmlSerializer serializer = new XmlSerializer( typeof( T ) );
			// 格納用バイト配列
			using( FileStream fs = new FileStream( path, FileMode.Open, FileAccess.Read ) )
			{
				// 逆シリアル化によりクラスに適用
				return (T)serializer.Deserialize( fs );
			}
		}
		catch
		{
			return default(T);
		}		
	}

	/// <summary>
	/// Xml書き込みメソッド
	/// </summary>
	/// <param name="path">書き込みを行うパス</param>
	/// <param name="obj">書き込むデータ</param>
	public static bool XmlSerialize<T>( string path, T obj )
	{
		// ファイルストリーム作成
		using( FileStream fs = new FileStream( path, System.IO.FileMode.Create ) )
		{
			// シリアライザ作成
			XmlSerializer serializer = new XmlSerializer( typeof( T ) );
			// シリアライズ
			serializer.Serialize( fs, obj );
			// ファイルストリームの破棄
			fs.Close();
		}

		return true;
	}
}
