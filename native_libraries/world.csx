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

    public double[] original_f0;
    public double[] f0;
    public double[] time_axis;
    public int f0_length;

    public double[,] spectrogram;
    public double[,] aperiodicity;
    public int fft_size;
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
        var apis = Manager.GetWorldCoreAPI();
        var tools = Manager.GetWorldToolsAPI();
        var world = new WorldSample();
        
        return new {
            initFromFile = (Func<object,Task<object>>)(
                async (inFileName) => 
                {
                    var x_length = tools.GetAudioLength(inFileName as string);
                    if (x_length <= 0) {
                        // File is not exist or file format is not mono?
                        return null;
                    }

                    double[] x = new double[x_length];
                    int fs, nbit;

                    tools.WavRead(inFileName as string, out fs, out nbit, x);

                    var parameters = new WorldParameters();
                    parameters.fs = fs;
                    parameters.frame_period = 5.0;

                    double[] y = null;

                    return new {
                        getFileInfo = (Func<object,Task<object>>)(
                            async (_) =>
                            {
                                var result = new {
                                    fs = fs,
                                    bit = nbit,
                                    sample = x_length,
                                    length = (double)x_length / fs
                                };

                                return result;
                            }
                        ),
                        analysis = (Func<object,Task<object>>)(
                            async (_) =>
                            {
                                world.F0EstimationHarvest(x, x_length, parameters);
                                world.SpectralEnvelopeEstimation(x, x_length, parameters);
                                world.AperiodicityEstimation(x, x_length, parameters);

                                return true;
                            }
                        ),
                        synthesis = (Func<object,Task<object>>)(
                            async (_) =>
                            {
                                var is_initiaize = false;
                                int y_length = (int)((parameters.f0_length - 1) * parameters.frame_period / 1000.0 * fs) + 1;

                                if (y == null) {
                                    y = new double[y_length];
                                    is_initiaize = true;
                                }
                                
                                System.Console.WriteLine(is_initiaize);
                                world.WaveformSynthesis(parameters, fs, y_length, y, is_initiaize);
                                
                                return true;
                            }
                        ),
                        saveToFile = (Func<object,Task<object>>)(
                            async (data) =>
                            {   
                                if (y == null) {
                                    return false;
                                }

                                var outFileName = ((IDictionary<string,object>)data)["fileName"] as string;

                                int y_length = (int)((parameters.f0_length - 1) * parameters.frame_period / 1000.0 * fs) + 1;
                                
                                tools.WavWrite(y, y_length, fs, nbit, outFileName);

                                return true;
                            }
                        ),
                        updateF0Points = (Func<object,Task<object>>)(
                            async (data) =>
                            {   
                                foreach (var kv in (IDictionary<string, object>)data)
                                {
                                    parameters.f0[Int32.Parse(kv.Key)] = Convert.ToDouble(kv.Value);
                                }

                                return true;
                            }
                        ),
                        getF0Length = (Func<object,Task<object>>)(
                            async (_) =>
                            {
                                return parameters.f0_length;
                            }
                        ),
                        getF0 = (Func<object,Task<object>>)(
                            async (_) =>
                            {
                                return parameters.f0;
                            }
                        )
                    };
                }
            )
        };
    }
}