namespace Graduation_Project.Services
{
    public interface IBackgroundJobScheduler
    {
        void EnqueueAnalysis(int labTestId);
    }
}
