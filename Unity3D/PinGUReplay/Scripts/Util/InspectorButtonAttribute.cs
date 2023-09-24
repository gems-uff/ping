using System;

namespace PinGUReplay.Util
{
    public enum ExecutionMode
    {
        Editor,
        Runtime,
        EditorAndRuntime
    }

    /// <summary>
    /// Put this attribute above one of your MonoBehaviour method and it will draw
    /// a button in the inspector automatically.
    ///
    /// NOTE: the method must not have any params and can not be static.
    /// <param name="title"> Optional title of the button.
    /// If omitted a beautified name of the method is used</param>
    /// <param name="mode"> Optional parameter that specifies when the button is clickable:
    /// only in editor, only in runtime (playmode) or both.
    /// By default the button is clickable in both editor and runtime</param>
    /// <code>
    /// <para>[Button]</para>
    /// <para>public void MyMethod()</para>
    /// <para>{</para>
    /// <para>    Debug.Log( "HELLO HELLO HELLO!!" );</para>
    /// <para>}</para>
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class InspectorButtonAttribute : Attribute
    {
        public readonly string title;
        public readonly ExecutionMode mode;

        public InspectorButtonAttribute() : this(string.Empty, ExecutionMode.EditorAndRuntime)
        {
        }

        public InspectorButtonAttribute(string title) : this(title, ExecutionMode.EditorAndRuntime)
        {
        }

        public InspectorButtonAttribute(ExecutionMode mode) : this(string.Empty, mode)
        {
        }

        public InspectorButtonAttribute(string title, ExecutionMode mode)
        {
            this.title = title;
            this.mode = mode;
        }
    }
}