using Quartz;
using Quartz.Spi;

namespace iOSBot.Bot.Quartz;

public class InternJobFactory(IServiceProvider serviceProvider) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return serviceProvider.GetService(bundle.JobDetail.JobType) as IJob;
    }

    public void ReturnJob(IJob job)
    {
        var disposable = job as IDisposable;
        disposable?.Dispose();
    }
}