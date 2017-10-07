using System.Threading.Tasks;

namespace MangleSocks.Core.Util.Threading.Tasks
{
    public static class CachedTasks
    {
        public static readonly Task<bool> TrueTask = Task.FromResult(true);
        public static readonly Task<bool> FalseTask = Task.FromResult(false);
    }
}