using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.UI;
public class FastNoiseLiteWithBurstUsageExample : MonoBehaviour
{
    [SerializeField] private FastNoiseLite.NoiseType noiseType;
    [SerializeField] private FastNoiseLite.CellularDistanceFunction cellularDistanceFunction;
    [SerializeField] private RawImage textureRawImage;
    [SerializeField] private float textureRoughness = 10;
    [SerializeField] private int seed = 0; 
    [SerializeField] private int2 sizeOfTexture = new int2(128,128);
  
   
    private Texture2D texture;
    void Update()
    {
        //Creates a new texture for visualizing the generated noise
        texture = new Texture2D(sizeOfTexture.x,sizeOfTexture.y,TextureFormat.RFloat,0,true);

        

        //Runs the noise calculation
        RunJobsAndBurstImplementation();
        
        textureRawImage.texture = texture;

    }
    public void RunJobsAndBurstImplementation(){
        
        //Monitors the execution time of the noise calculation
        var Stopwatch = System.Diagnostics.Stopwatch.StartNew();

        //Initializes a array for storing the calculated data
        NativeArray<float> noiseData = new NativeArray<float>(sizeOfTexture.x*sizeOfTexture.y,Allocator.Persistent);


        //Differently from the original FastNoiseLite library, you must provide a seed on the constructor, otherwise the calculations will not work properly.
        //If the seed don't matter to you, you can set it to 0.
        FastNoiseLite fastNoiseLite = new FastNoiseLite(seed);

        fastNoiseLite.SetNoiseType(noiseType);
        fastNoiseLite.SetCellularDistanceFunction(cellularDistanceFunction);
        
        //Initializes a job and pass all the necessary data
        NoiseCalculationJob noiseJob = new NoiseCalculationJob(){
          NoiseData  = noiseData,
          NoiseDimensions = sizeOfTexture,
          Seed = seed,
          Roughness = textureRoughness,
          fastNoiseLite = fastNoiseLite
        };

        /// Schedule the job and wait it to complete.
        /// Instead of waiting it to finish right away, you can do other calculations on the main thread while the job is running and then complete it just when you actualy need the data.
        noiseJob.Schedule(noiseData.Length,1).Complete();
        
        //Logs the time that took to calculate the noise
        Stopwatch.Stop();
        Debug.Log($"Jobs+Burst time:{Stopwatch.ElapsedMilliseconds}ms");


        /// Assigns the calculated data to the texture.It is assigned to a Rfloat texture, so it's not needed any type of conversion, saving compute time
        texture.SetPixelData<float>(noiseData,0,0);
        texture.Apply();

        ///Always dispose native arrays when you dont need the data anymore.
        noiseData.Dispose();

    }
    private Color floatToColor(float num){
        return new Color(num,num,num,1);
    }
}
[BurstCompile]
public struct NoiseCalculationJob : IJobParallelFor
{
    public NativeArray<float> NoiseData;
    public int2 NoiseDimensions;
    public int Seed;
    public float Roughness;
    public FastNoiseLite fastNoiseLite;
    public void Execute(int i)
    {
        //The IJobParalelFor will run a for loop that indexes the NoiseData array, but instead of running one index each time, multiple of them are calculated in parallel, finishing the calculation much quicker.
        //The 'i' variable is the index of the NoiseData array that is currently being calculated by the job. But the FastNoiseLite library needs the coordinates of a point in the texture that you want to calculate, so the following code calculates it.

        int xCoord = (int)((i+1)%NoiseDimensions.y)-1;
        int yCoord = (int)math.floor((i+1)/NoiseDimensions.x)-1;

        //Now the noise is finally calculated and mapped to a range of 0 to 1 instead of -1 to 1.
        NoiseData[i] = math.unlerp(-1,1,fastNoiseLite.GetNoise(xCoord*Roughness,yCoord*Roughness));
        
    }
}
