using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LayerTest/LayerTestSo")]
public class LayerTestSo : ScriptableObject
{
    public LayerMask mask;
    public NestedMaskData nested;
    
    public LayerMask[] maskArray;
    public List<NestedMaskData> nestedList;
    
    [SerializeReference] public NestedMaskData referenceNested;
    [SerializeReference] public List<NestedMaskData> referenceNestedList;
}