namespace BotwTrainer
{
    public enum Command
    {
        WRITE_8 = 0x01,

        WRITE_16 = 0x02,

        WRITE_32 = 0x03,

        MEMORY_READ = 0x04,

        READ_MEMORY_KERNEL = 0x05,

        VALIDATE_ADDRESS_RANGE = 0x06,

        // DISASSEMBLE_RANGE = 0x07,

        MEMORY_DISASSEMBLE = 0x08,

        READ_MEMORY_COMPRESSED = 0x09,

        MEMORY_KERNEL_WRITE = 0x0B,

        MEMORY_KERNEL_READ = 0x0C,

        TAKE_SCREEN_SHOT = 0x0D,

        MEMORY_UPLOAD = 0x41,

        DATA_BUFFER_SIZE = 0x51,

        DUMP_REMOTE_FILE = 0x52,

        DUMP_REMOTE_DIRECTORY = 0x53,

        REPLACE_REMOTE_FILE = 0x54,

        CODE_HANDLER_INSTALLATION_ADDRESS = 0x55,

        READ_THREADS = 0x56,

        ACCOUNT_IDENTIFIER = 0x57,

        WRITE_SCREEN = 0x58,

        FOLLOW_POINTER = 0x60,

        RPC = 0x70,

        GET_SYMBOL = 0x71,

        MEMORY_SEARCH_32 = 0x72,

        MEMORY_SEARCH = 0x73,

        GET_SERVER_VERSION = 0x99,

        GET_OS_VERSION = 0x9A,

        MEMORY_READ_COMPRESSED = 0x09,

        START_KERNEL_COPY_THREAD = 0xCD
    }
}