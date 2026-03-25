using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CentipedeBoneSetup : MonoBehaviour
{
    [SerializeField] private Transform rigContainer;
    [SerializeField] private Transform startBone;
    [SerializeField] private Transform endBone;

    public void SetupBones()
    {
        Transform sourceObject = startBone;
        Transform constrainedObject = startBone.parent;

        int index = 0;

        while (sourceObject != endBone && constrainedObject != null)
        {
            GameObject boneRigObject = new GameObject($"Dampen Move Bone {index}");
            boneRigObject.transform.SetParent(rigContainer);

            DampedTransform boneRig = boneRigObject.AddComponent<DampedTransform>();
            boneRig.data.sourceObject = sourceObject;
            boneRig.data.constrainedObject = constrainedObject;

            sourceObject = constrainedObject;
            constrainedObject = sourceObject.parent;
            index++;
        }
    }
}
