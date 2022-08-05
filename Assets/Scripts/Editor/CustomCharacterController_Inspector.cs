using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(PlayerStateMachine))]
public class CustomCharacterController_Inspector : Editor
{
    public VisualTreeAsset m_InspectorXML;

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement myInspector = new VisualElement();

        m_InspectorXML.CloneTree(myInspector);

        //VisualElement inspectorFoldout = myInspector.Q("Default_Inspector");

        //InspectorElement.FillDefaultInspector(inspectorFoldout, serializedObject, this);

        return myInspector;
    }
}
