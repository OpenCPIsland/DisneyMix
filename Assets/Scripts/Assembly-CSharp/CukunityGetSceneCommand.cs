using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CukunityGetSceneCommand : CukunityCommand
{
	public delegate void SerializerMethod(Component c, Hashtable h);

	private static Utility u = new Utility();

	public override void Process(Hashtable req, Hashtable res)
	{
		List<GameObject> list = new List<GameObject>();
		foreach (Selectable allSelectable in Selectable.allSelectablesArray)
		{
			list.Add(allSelectable.gameObject);
		}
		List<Hashtable> context = (List<Hashtable>)(res["gameObjects"] = new List<Hashtable>());
		foreach (GameObject item in list)
		{
			Traverse(item, SerializeGameObject, context);
		}
	}

	private object SerializeGameObject(GameObject obj, object context)
	{
		Hashtable hashtable = new Hashtable();
		string name = obj.name;
		hashtable["name"] = name;
		hashtable["children"] = new List<Hashtable>();
		hashtable["components"] = SerializeComponents(obj);
		List<Hashtable> list = context as List<Hashtable>;
		if (list != null)
		{
			list.Add(hashtable);
		}
		return hashtable["children"];
	}

	private List<Hashtable> SerializeComponents(GameObject obj)
	{
		List<Hashtable> list = new List<Hashtable>();
		Component[] components = obj.GetComponents<Component>();
		Component[] array = components;
		foreach (Component comp in array)
		{
			Hashtable hashtable = new Hashtable();
			SerializeComponent(comp, hashtable);
			if (hashtable.Count != 0)
			{
				list.Add(hashtable);
			}
		}
		return list;
	}

	public static string GetComponentTypeString(Component comp, Type t = null)
	{
		if (t == null)
		{
			t = comp.GetType();
		}
		string text = t.ToString();
		if (text == "UnityEngine.Behaviour")
		{
			int num = comp.ToString().LastIndexOf("(UnityEngine.");
			if (num >= 0)
			{
				string text2 = comp.ToString().Substring(num);
				if (text2.Length >= "(UnityEngine.x)".Length && text2.EndsWith(")"))
				{
					text = text2.Substring(1, text2.Length - 2);
				}
			}
		}
		if (text.StartsWith("UnityEngine."))
		{
			text = text.Remove(0, "UnityEngine.".Length);
		}
		return text;
	}

	public static void SerializeComponent(Component comp, Hashtable compHash)
	{
		Type type = comp.GetType();
		Dictionary<Type, SerializerMethod> dictionary = new Dictionary<Type, SerializerMethod>();
		dictionary.Add(typeof(RectTransform), SerializeRectTransform);
		dictionary.Add(typeof(Text), SerializeGUIText);
		Dictionary<Type, SerializerMethod> dictionary2 = dictionary;
		foreach (KeyValuePair<Type, SerializerMethod> item in dictionary2)
		{
			if (type.IsSubclassOf(item.Key))
			{
				item.Value(comp, compHash);
			}
		}
		foreach (KeyValuePair<Type, SerializerMethod> item2 in dictionary2)
		{
			if (type == item2.Key)
			{
				item2.Value(comp, compHash);
			}
		}
	}

	public static void SerializeTransform(Component comp, Hashtable h)
	{
		Transform transform = comp as Transform;
		h["position"] = SerializeVector3(transform.position);
	}

	public static void SerializeRectTransform(Component comp, Hashtable h)
	{
		RectTransform rectTransform = comp as RectTransform;
		Rect rectInPhysicalScreenSpace = u.GetRectInPhysicalScreenSpace(rectTransform);
		Hashtable hashtable = new Hashtable();
		hashtable.Add("x", rectInPhysicalScreenSpace.x + rectInPhysicalScreenSpace.width / 2f);
		hashtable.Add("y", rectInPhysicalScreenSpace.y + rectInPhysicalScreenSpace.height / 2f);
		h["position"] = hashtable;
		Component[] components = rectTransform.GetComponents<Component>();
		foreach (Component component in components)
		{
			if (component.GetType().ToString().Equals("UnityEngine.UI.Text"))
			{
				h["text"] = ((Text)component).text;
				break;
			}
		}
	}

	public static void SerializeCamera(Component comp, Hashtable h)
	{
	}

	public static void SerializeGUIElement(Component comp, Hashtable h)
	{
		RectTransform rectTransform = comp as RectTransform;
		if (rectTransform == null)
		{
			return;
		}
		AppendHint(h, "screen", SerializeRect(u.GetRectInPhysicalScreenSpace(rectTransform)));
	}

	public static void SerializeGUIText(Component comp, Hashtable h)
	{
		Text textComponent = comp as Text;
		if (textComponent == null)
		{
			return;
		}
		h["text"] = textComponent.text;
		AppendHint(h, "text", textComponent.text);
	}

	public static Hashtable AppendHint<T>(Hashtable h, string name, T hint)
	{
		Hashtable hashtable = h[".hints"] as Hashtable;
		if (hashtable == null)
		{
			hashtable = (Hashtable)(h[".hints"] = new Hashtable());
		}
		List<T> list = (List<T>)(hashtable.Contains(name) ? (hashtable[name] as List<T>) : (hashtable[name] = new List<T>()));
		list.Add(hint);
		hashtable["hint"] = "hint";
		return hashtable;
	}

	public static Hashtable SerializeRect(Rect r)
	{
		Hashtable hashtable = new Hashtable();
		hashtable["x"] = r.x;
		hashtable["y"] = r.y;
		hashtable["width"] = r.width;
		hashtable["height"] = r.height;
		return hashtable;
	}

	public static Hashtable SerializeVector3(Vector3 v)
	{
		Hashtable hashtable = new Hashtable();
		hashtable["x"] = v.x;
		hashtable["y"] = (float)Screen.height - v.y;
		hashtable["z"] = v.z;
		return hashtable;
	}

	public static Hashtable SerializeVector2(Vector2 v)
	{
		Hashtable hashtable = new Hashtable();
		hashtable["x"] = v.x;
		hashtable["y"] = v.y;
		return hashtable;
	}

	public static Hashtable SerializeQuaternion(Quaternion q)
	{
		Hashtable hashtable = new Hashtable();
		hashtable["x"] = q.x;
		hashtable["y"] = q.y;
		hashtable["z"] = q.z;
		hashtable["w"] = q.w;
		return hashtable;
	}

	public static float[] SerializeMatrix4x4(Matrix4x4 m)
	{
		float[] array = new float[16];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = m[i];
		}
		return array;
	}
}
