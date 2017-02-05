//#r "native_libraries/mac/DotnetWorld.dll"
//#r "System.Core.dll"
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using DotnetWorld.API.Common.Struct;
using DotnetWorld.API.Common;

public class WorldParameters
{
    public double frame_period;
    public int fs;
    public int nbit;

    public double[] original_f0;
    public double[] f0;
    public double[] time_axis;
    public int f0_length;

    public double[,] spectrogram;
    public double[,] aperiodicity;
    public int fft_size;

    public int x_length;
    public double[] x;
    
    public int y_length;
    public double[] y;
}

public class WorldSample
{
    public void F0EstimationDio(double[] x, int x_length, WorldParameters world_parameters)
    {
        var option = new DioOption();
        var apis = Manager.GetWorldCoreAPI();

        apis.InitializeDioOption(option);

        option.frame_period = world_parameters.frame_period;
        option.speed = 1;
        option.f0_floor = 71.0;
        option.allowed_range = 0.1;

        world_parameters.f0_length = apis.GetSamplesForDIO(world_parameters.fs,
            x_length, world_parameters.frame_period);
        world_parameters.original_f0 = new double[world_parameters.f0_length];
        world_parameters.f0 = new double[world_parameters.f0_length];
        world_parameters.time_axis = new double[world_parameters.f0_length];
        double[] refined_f0 = new double[world_parameters.f0_length];

        apis.Dio(x, x_length, world_parameters.fs, option, world_parameters.time_axis,
            world_parameters.original_f0);
        
        apis.StoneMask(x, x_length, world_parameters.fs, world_parameters.time_axis,
            world_parameters.original_f0, world_parameters.f0_length, refined_f0);

        System.Array.Copy(refined_f0, 0, world_parameters.original_f0, 0, world_parameters.f0_length);
        System.Array.Copy(refined_f0, 0, world_parameters.f0, 0, world_parameters.f0_length);
    }

    public void F0EstimationHarvest(double[] x, int x_length, WorldParameters world_parameters)
    {
        var option = new HarvestOption();
        var apis = Manager.GetWorldCoreAPI();

        apis.InitializeHarvestOption(option);
        
        option.frame_period = world_parameters.frame_period;
        option.f0_floor = 71.0;

        world_parameters.f0_length = apis.GetSamplesForDIO(world_parameters.fs,
            x_length, world_parameters.frame_period);
        world_parameters.original_f0 = new double[world_parameters.f0_length];
        world_parameters.f0 = new double[world_parameters.f0_length];
        world_parameters.time_axis = new double[world_parameters.f0_length];
        
        apis.Harvest(x, x_length, world_parameters.fs, option,
            world_parameters.time_axis, world_parameters.original_f0);

        System.Array.Copy(world_parameters.original_f0, 0,
            world_parameters.f0, 0, world_parameters.f0_length);
    }

    public void SpectralEnvelopeEstimation(double[] x, int x_length, WorldParameters world_parameters)
    {
        var option = new CheapTrickOption();
        var apis = Manager.GetWorldCoreAPI();

        apis.InitializeCheapTrickOption(world_parameters.fs, option);

        option.q1 = -0.15;
        option.f0_floor = 71.0;

        world_parameters.fft_size = apis.GetFFTSizeForCheapTrick(world_parameters.fs, option);
        world_parameters.spectrogram = new double[world_parameters.f0_length, world_parameters.fft_size / 2 + 1];
        
        apis.CheapTrick(x, x_length, world_parameters.fs, world_parameters.time_axis,
            world_parameters.original_f0, world_parameters.f0_length, option,
            world_parameters.spectrogram);
    }

    public void AperiodicityEstimation(double[] x, int x_length, WorldParameters world_parameters)
    {
        var option = new D4COption();
        var apis = Manager.GetWorldCoreAPI();

        apis.InitializeD4COption(option);
        option.threshold = 0.85;

        world_parameters.aperiodicity = new double[world_parameters.f0_length, world_parameters.fft_size / 2 + 1];

        apis.D4C(x, x_length, world_parameters.fs, world_parameters.time_axis,
            world_parameters.original_f0, world_parameters.f0_length,
            world_parameters.fft_size, option, world_parameters.aperiodicity);
    }

    public void WaveformSynthesis(WorldParameters world_parameters, int fs, int y_length, double[] y, bool original = true)
    {
        var apis = Manager.GetWorldCoreAPI();
        apis.Synthesis(original ? world_parameters.original_f0: world_parameters.f0,
            world_parameters.f0_length,
            world_parameters.spectrogram, world_parameters.aperiodicity,
            world_parameters.fft_size, world_parameters.frame_period, fs,
            y_length, y);
    }
}

public class Startup
{
    public async Task<object> Invoke(object param)
    {        
        return new {
            // when faild initialize,  return null
            // succeeded, return file instance
            initFromFile = (Func<object,Task<object>>)(async (inFileName) => 
            {
                var fileInstance = new WorldAPIWrapper();
                var isValid = await fileInstance.InitializeFromFile(inFileName as string);

                if (!isValid) {
                    return null;
                }

                return new {
                    // return file information dictionary
                    getFileInfo = (Func<object,Task<object>>)(async (_) =>
                    {
                        return await fileInstance.GetFileInfo();
                    }),
                    // always return true
                    analysis = (Func<object,Task<object>>)(async (_) =>
                    {
                        return await fileInstance.Analysis();
                    }),
                    // always return true
                    synthesis = (Func<object,Task<object>>)(async (_) =>
                    {        
                        return await fileInstance.Synthesis();
                    }),
                    // parameter: fileName -> save file name
                    //            overwrite -> if this parameter is false, append current time to file　name
                    // return saved file name when succeeded, else blank string
                    saveToFile = (Func<object,Task<object>>)(async (outFileParameters) =>
                    {   
                        var _param = (IDictionary<string,object>)outFileParameters;

                        object dictvalue;
                        
                        string outFileName = "";
                        bool do_overwrite = true;

                        if (!_param.TryGetValue("fileName", out dictvalue)) {
                            return "";
                        }
                        outFileName = (string)dictvalue;
                        
                        if (_param.TryGetValue("overwrite", out dictvalue)) {
                            do_overwrite = (bool)dictvalue;
                        }
                        
                        return await fileInstance.SaveToFile(outFileName, do_overwrite);
                    }),
                    // parameter: f0WithIdx -> dictionary which key is index, value is new f0
                    updateF0Points = (Func<object,Task<object>>)(async (f0WithIdx) =>
                    {   
                        return await fileInstance.UpdateF0Points((IDictionary<string, object>)f0WithIdx);
                    }),
                    // return f0 array
                    getF0 = (Func<object,Task<object>>)(async (_) =>
                    {
                        return await fileInstance.GetF0();
                    })
                };
            })
        };
    }
}

public class WorldAPIWrapper
{
    private ICore Apis;
    private ITools Tools;
    private WorldSample World;
    private WorldParameters Parameters;

    public WorldAPIWrapper()
    {
        Apis = Manager.GetWorldCoreAPI();
        Tools = Manager.GetWorldToolsAPI();
        World = new WorldSample();
        Parameters = new WorldParameters();
    }

    async public Task<bool> InitializeFromFile(string fileName)
    {
        var x_length = Tools.GetAudioLength(fileName);
        if (x_length <= 0) {
            // File is not exist or file format is not mono
            return false;
        }

        Parameters.x = new double[x_length];
        
        Tools.WavRead(fileName, out Parameters.fs, out Parameters.nbit, Parameters.x);

        Parameters.x_length = x_length;
        Parameters.frame_period = 5.0;

        return true;
    }

    async public Task<object> GetFileInfo()
    {
        return new {
            fs = Parameters.fs,
            bit = Parameters.nbit ,
            sample = Parameters.x_length,
            length = (double)(Parameters.x_length / Parameters.fs)
        };
    }

    async public Task<bool> Analysis()
    {
        World.F0EstimationHarvest(Parameters.x, Parameters.x_length, Parameters);
        World.SpectralEnvelopeEstimation(Parameters.x, Parameters.x_length, Parameters);
        World.AperiodicityEstimation(Parameters.x, Parameters.x_length, Parameters);

        return true;
    }

    async public Task<bool> Synthesis()
    {
        var y_length = (int)((Parameters.f0_length - 1) * Parameters.frame_period / 1000.0 * Parameters.fs) + 1;
        var is_initiaize = false;

        if (Parameters.y == null) {
            Parameters.y = new double[y_length];
            Parameters.y_length = y_length;
            is_initiaize = true;
        }
        
        World.WaveformSynthesis(Parameters, Parameters.fs, y_length, Parameters.y, is_initiaize);

        return true;
    }

    async public Task<string> SaveToFile(string fileName, bool overwrite = true)
    {
        if (Parameters.y == null) {
            return "";
        }

        var savedFileName = fileName;

        if (!overwrite && System.IO.File.Exists(fileName)) {
            var dir = System.IO.Path.GetDirectoryName(fileName);
            var base_file_name = System.IO.Path.GetFileNameWithoutExtension(fileName);
            var extension = System.IO.Path.GetExtension(fileName);
            var now = System.DateTime.Now.ToString("_yyyyMMddhhmmss");
            savedFileName = dir + System.IO.Path.DirectorySeparatorChar + base_file_name + now + extension;
        }
        
        Tools.WavWrite(Parameters.y, Parameters.y_length, Parameters.fs, Parameters.nbit, savedFileName);

        return savedFileName;
    }

    async public Task<bool> UpdateF0Points(IDictionary<string, object> data)
    {
        foreach (var kv in data)
        {
            Parameters.f0[Int32.Parse(kv.Key)] = Convert.ToDouble(kv.Value);
        }
        return true;
    }

    async public Task<object> GetF0()
    {
        return Parameters.f0;
    }
}
