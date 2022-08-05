using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonStackBoxes : IInteractable
{
    [SerializeField] public int m_rowCount;
    [SerializeField] public int m_columCount;
    [SerializeField] public float m_boxSize;
    [SerializeField] GameObject m_spawnPoint;




    private List<GameObject> m_cubes = new List<GameObject>();
    private DamageableTest m_damage;

    [SerializeField] public Color HighlightColor { get => _highlightColor; set => _highlightColor = value; }
    [SerializeField] public float EmissionIntensity { get => _emissionIntensity; set => _emissionIntensity = value; }

    public override bool Interact(Interactor interactor)
    {
        foreach(GameObject cube in m_cubes)
        {
            Object.Destroy(cube);
        }

        for(int r = 0; r < m_rowCount; r++)
        {
            for(int c = 0; c < m_columCount; c++)
            {
                Vector3 position = m_spawnPoint.transform.position + (Vector3.right * m_boxSize* r) + (Vector3.up * m_boxSize * c);  
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = position;
                cube.transform.localScale = new Vector3(m_boxSize, m_boxSize, m_boxSize);
                cube.AddComponent<Rigidbody>();
                var damageable = cube.AddComponent<DamageableTest>().m_maxHealth = 100 ;
                m_cubes.Add(cube);
            }
        }

        return true;
    }
}
