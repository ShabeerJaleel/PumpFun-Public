using System.Threading.Channels;

namespace PumpFun.Core.Models
{
    public class TokenCreationChannel
    {
        private readonly Channel<TransactionModel> _channel;
        
        public TokenCreationChannel()
        {
            _channel = Channel.CreateUnbounded<TransactionModel>();
        }

        public ChannelWriter<TransactionModel> Writer => _channel.Writer;
        public ChannelReader<TransactionModel> Reader => _channel.Reader;
    }

    public class TradeChannel
    {
        private readonly Channel<TransactionModel> _channel;
        
        public TradeChannel()
        {
            _channel = Channel.CreateUnbounded<TransactionModel>();
        }

        public ChannelWriter<TransactionModel> Writer => _channel.Writer;
        public ChannelReader<TransactionModel> Reader => _channel.Reader;
    }
}
