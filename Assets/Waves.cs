using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Waves : MonoBehaviour
{
	public int XSize = 250;
	public int ZSize = 250;
	public float YScale = 10.0f;

	public float Speed = 1.0f;
	public float Resolution = 10;

	private MeshFilter meshFilter;
	private NativeArray<Vector3> verticesNativeArray;
	private Vector3[] modifiedVertices;

	private void Start ()
	{		
		var mesh = new Mesh();
		meshFilter = GetComponent<MeshFilter>();
        
		modifiedVertices = new Vector3[(XSize + 1) * (ZSize + 1)];
		Vector2[] uv = new Vector2[modifiedVertices.Length];

		verticesNativeArray = new NativeArray<Vector3>(modifiedVertices, Allocator.Persistent);

		var position = transform.position;
		for (int i = 0, z = 0; z <= ZSize; z++)
		{
			for (int x = 0; x <= XSize; x++, i++)
			{
				modifiedVertices[i] = new Vector3(x + position.x, position.y, z + position.z);
				uv[i] = new Vector2((float) x / XSize, (float) z / ZSize);
			}
		}

		mesh.name = "Waves";
		mesh.vertices = modifiedVertices;
		mesh.uv = uv;
        
		int[] triangles = new int[XSize * ZSize * 6];
		for (int ti = 0, vi = 0, y = 0; y < ZSize; y++, vi++) 
		{
			for (int x = 0; x < XSize; x++, ti += 6, vi++)
			{
				triangles[ti] = vi;
				triangles[ti + 3] = triangles[ti + 2] = vi + 1;
				triangles[ti + 4] = triangles[ti + 1] = vi + XSize + 1;
				triangles[ti + 5] = vi + XSize + 2;
			}
		}
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		
		meshFilter.mesh = mesh;
	}

	private void Update ()
	{
		var randomizer = -Time.time / 2;

		if (Resolution <= 0)
		{
			Resolution = 0;
		}
		
		verticesNativeArray.CopyFrom(modifiedVertices);

		var job = new CalculateWavesJob();
		job.Vertices = verticesNativeArray;
		job.BasePosition = transform.position;
		job.Randomizer = randomizer;
		job.Resolution = Resolution;
		job.Speed = Speed;
		job.YScale = YScale;

		var handle = job.Schedule(modifiedVertices.Length, 64);
		handle.Complete();
		
		job.Vertices.CopyTo(modifiedVertices);

		
	}

	private void LateUpdate()
	{
		meshFilter.mesh.vertices = modifiedVertices;
		meshFilter.mesh.RecalculateBounds();
		meshFilter.mesh.RecalculateNormals();
	}

	private void OnDestroy()
	{
		verticesNativeArray.Dispose();
	}
}

struct CalculateWavesJob : IJobParallelFor
{
	public NativeArray<Vector3> Vertices;

	public float Randomizer;
	public float YScale;
	public float Speed;
	public float Resolution;
	public Vector3 BasePosition;
	
	public void Execute(int index)
	{
		Vector3 vertex = Vertices[index];
		vertex.y = Mathf.PerlinNoise((Speed * Randomizer) + (vertex.x + BasePosition.x) / Resolution,
			           -(Speed * Randomizer) + (vertex.z + BasePosition.z) / Resolution
		           ) * YScale;

		Vertices[index] = vertex;
	}
}
