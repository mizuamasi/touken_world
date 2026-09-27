using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ReadImage
{
	static public Sprite ReadSprite(string path) {
		Texture2D tex = ReadTexture(path);
		return Sprite.Create(
			tex, 
			new Rect(0, 0, tex.width, tex.height), 
			new Vector2(0.5f, 0.5f));
	}

	static public Texture2D ReadTexture(string path)
    {
        byte[] readBinary = ReadFile(path);

        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(readBinary);

        return texture;
    }

    static public byte[] ReadFile(string path)
    {
        FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        BinaryReader bin = new BinaryReader(fileStream);
        byte[] values = bin.ReadBytes((int)bin.BaseStream.Length);

        bin.Close();

        return values;
    }
}
