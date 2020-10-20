////////////////////////////////////////////////////////////////////////
// Project Name    : Game of life 3D
// File Name       : GameofLife3D.cs
// Creation Date   : 2020/10/20
// Lisence         : MIT
// 
// Copyright © 2020 taki corporation. All rights reserved.
////////////////////////////////////////////////////////////////////////
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class GameofLife3D : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private Mesh instanceMesh; // セルのメッシュ
    [SerializeField] private Material incetanceMaterial;
    [SerializeField] private Color birthColor = Color.green; // 誕生したセルの色 
    [SerializeField] private Color deathColor = Color.red; // 死んだセルの色
    [SerializeField] private Color stayAliveColor = Color.black; // 生の持続の色
    [SerializeField] private Color emission = Color.white; // エミッションの色
    [SerializeField] private int columns = 50; // セルの行
    [SerializeField] private int rows = 50; // セルの列
    [SerializeField] private int generation = 200; // セルの世代
    [SerializeField] private int threshold = 100; // 崩壊させる世代の閾値
    [SerializeField] private Vector3 gravity = new Vector3(0f, -0.01f, 0f); // 重力
    [SerializeField] private float drag = 0.996f; // 空気抵抗

    private int generationCount = 0; // 世代数
    
    private int kernelId = 0;
    private int subMeshIndex = 0;
    private ComputeBuffer[] cellPingPongBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] {0, 0, 0, 0, 0,};

    // 全セルの個数
    public int InstanceCount
    {
        get { return columns * rows * generation; }
    }

    // セルの構造体
    struct Cell
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector4 color;
        public Vector4 emission;
        public int state;
        public int display;
        public float scale;
    }

    private void Start()
    {
        // セルを作成し、バッファを初期化する
        InitBuffers();
    }

    private void Update()
    {
        // セルを更新する
        UpdateCellKernel();
        
        // トランスフォームの行列をシェーダに送信する
        incetanceMaterial.SetMatrix("_World2Local", transform.worldToLocalMatrix);
        incetanceMaterial.SetMatrix("_Local2World", transform.localToWorldMatrix);
        // セルのバッファをシェーダに送信する
        incetanceMaterial.SetBuffer("_CellBuffer", cellPingPongBuffer[0]);

        // レンダリングする
        Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, incetanceMaterial,
            new Bounds(Vector3.zero, Vector3.one * 32), argsBuffer);
        
        // Rキーでリセットする
        if (Input.GetKey(KeyCode.R))
        {
            generationCount = 0;
            InitBuffers();
        }
    }

    // コンピュートシェーダでセルを更新する
    private void UpdateCellKernel()
    {
        // 子孫を増加させる
        generationCount++;

        // コンピュートシェーダに情報を送信する
        kernelId = computeShader.FindKernel("UpdateCell");
        computeShader.SetBuffer(kernelId, "CellBuffer", cellPingPongBuffer[0]);
        computeShader.SetBuffer(kernelId, "CellBufferWrite", cellPingPongBuffer[1]);
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        computeShader.SetInt("_Columns", columns);
        computeShader.SetInt("_Rows", rows);
        computeShader.SetInt("_Generation", generation);
        computeShader.SetInt("_GenerationCount", generationCount);
        computeShader.SetInt("_Threshold", threshold);
        computeShader.SetVector("_Gravity", gravity);
        computeShader.SetFloat("_Drag", drag);
        computeShader.SetVector("_BirthColor", birthColor);
        computeShader.SetVector("_DeathColor", deathColor);
        computeShader.SetVector("_StayAliveColor", stayAliveColor);
        computeShader.SetVector("_Emission", emission);
        computeShader.Dispatch(kernelId, InstanceCount / 10, 1, 1);
    
        // セルのバッファの新旧を入れ替える
        Swap();
    }

    // セルを作成し、バッファを初期化する
    private void InitBuffers()
    {
        
        // セルを作成する
        Cell[] cells = new Cell[InstanceCount];

        for (int y = 0; y < generation; y++)
        {
            for (int z = 0; z < rows; z++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int idx = (y * columns * rows) + (z * columns) + x;
                    cells[idx] = new Cell
                    {
                        position = new Vector3(x - columns / 2, y, z - rows / 2),
                        velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)),
                        color = Color.clear,
                        emission = Color.clear,
                        state = y == 0 ? (int) (Random.value * 2.0f) : 0,
                        display = 0,
                        scale = 1,
                    };
                }
            }
        }
        
        // セルのバッファを初期化し、セルのデータをセットする
        cellPingPongBuffer = new ComputeBuffer[2];
        cellPingPongBuffer[0] = new ComputeBuffer(InstanceCount, Marshal.SizeOf(typeof(Cell)));
        cellPingPongBuffer[1] = new ComputeBuffer(InstanceCount, Marshal.SizeOf(typeof(Cell)));

        cellPingPongBuffer[0].SetData(cells);
        cellPingPongBuffer[1].SetData(cells);
        
        // DrawMeshInstancedIndirectでレンダリング時に使うバッファを初期化し、データをセットする
        args[0] = (uint) instanceMesh.GetIndexCount(subMeshIndex);
        args[1] = (uint) InstanceCount;
        args[2] = (uint) instanceMesh.GetIndexStart(subMeshIndex);
        args[3] = (uint) instanceMesh.GetBaseVertex(subMeshIndex);

        argsBuffer = new ComputeBuffer(1, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
    }

    // セルのバッファの新旧を入れ替える
    private void Swap()
    {
        ComputeBuffer tmp = cellPingPongBuffer[0];
        cellPingPongBuffer[0] = cellPingPongBuffer[1];
        cellPingPongBuffer[1] = tmp;
    }

    // 使用しなくなったバッファは開放する
    private void OnDisable()
    {
        foreach (var buf in cellPingPongBuffer)
        {
            buf.Release();
        }

        argsBuffer.Release();
    }
}