# dbcexplorer #

A C# reader and explorer of World of Warcraft game data files
(from `.dbc` and `.db2` files).

There are two parts to this project:
 - DbcReader - a lightweight class library that allows developers to read .dbc files and interact with their records in a meaningful way.
 - DbcExplorer - a grid-based viewer for .dbc files that allows you to decide what columns mean. 

## The DBC File Format ##

At their core, DBC files are fixed-width rows of 4-byte columns.  The file format indicates the number of columns per row as part of the header.  Each column is generally one of the following broad categories:

 - 32-bit integer
 - 32-bit floating point (single-precision float)
 - String
 - Boolean
 - Flags, a variation of 32-bit integer

Each of these can be represented by a 32-bit column value, with the exception of strings.  Strings are a 32-bit offset into the string table, which is found at the end of the DBC file at an offset specified in the header.

### Using DbcExplorer to understand a DBC file ###

DbcExplorer can open a .dbc file, enumerate its columns, and observe the size of its string table.  By casual inspection, a user can sometimes determine what kinds of values a column contains; if a column has many large integers of seemingly random values, it's probably a floating-point column.  If it has integers that generally progress upwards by small amounts (10-100 or so per row), that's probably a string column.  If a column only has 1 or 0, it's probably a Boolean column, etc.  If a column frequently has the same set of seemingly-random numbers in it, it may be a flags column.

By trial and error, you can typically get reasonably good analyses of a given DBC or DB2 file.  You can name the columns and give them types.  Once you have done this, you can save the schema (.dbcschema) to a file, so the next time you open the file, you can also load the schema to start off.

At the time of this writing, I do not appear to have the source code that generated a `DbcTable<T>` entity from a `.dbcschema`.  It is fairly straightforward to author using the `System.CodeDom` APIs, particularly when using the DbcReader types.  The `ChatProfanityRecord.cs` file in the DbcReader project is an example of what is generated.

### Reading DBC files from a C# program ###

If all you're after is reading DBC files programmatically, then you'll want to use the DbcReader project instead.  It provides a base `DbcTable` type as well as a specialized `DbcTable<T>` type.  The latter will dynamically compile a reader for the `T` type, so long as it conforms to the type system outlined above.

`DbcTable<T>` knows how to read the following types:

 - int (System.Int32)
 - float (System.Single)
 - string (System.String)
 - DbcReader.DbcStringReference

Again, it appears that what I have is an out-of-date version of my code.  It should be trivial to extend this to support 32-bit enumerations as well.

A `DbcStringReference` is a lazily-evaluated string that can be turned into a .NET string as long as the `DbcTable<T>` from which is was read has not been garbage-collected or disposed.  This allows records to be perused without immediately invoking the cost of decoding a UTF-8 string and storing it in memory.

The ChatProfanityRecord class is an example of such an entity:

    public class ChatProfanityRecord
    {
        [DbcRecordPosition(0)]
        public int ID { get; set; }
        [DbcRecordPosition(1)]
        public DbcStringReference DirtyWord;
        [DbcRecordPosition(2)]
        public int LanguageID;
    }

The `DbcRecordPosition` attributes allow you to skip columns you don't know or don't care about, treating them as sparse.  It can be applied to either fields or read-write properties; you can see above that ID is a property, but DirtyWord and LanguageID are fields.  Both are equally acceptable.

Internally, `DbcTableCompiler` creates dynamic methods which internally look like the equivalent C# code had been written.  For example, for a `ChatProfanityRecord`, a new DynamicMethod is created that might look like:

    void $DbcTable$DbcReader$ChatProfanityRecord(BinaryReader reader, int row, DbcTable source, ChatProfanityRecord target)
    {
        target.ID = reader.ReadInt32();
        target.DirtyWord = new DbcStringReference(source, reader.ReadInt32());
        target.LanguageID = reader.ReadInt32();
    }

This makes the reading highly efficient and fast.

## What can you do with it? ##

I used it to crawl through items to extract icons and view achievement trees.  I had used that to create a WoW Character Profile app for Windows Phone (since Armory isn't there), but then the introduction of CASC was a moderate blocker.  I never quite got CascLib working the same way I'd gotten StormLib working; I probably could now that some time has passed, so I may re-open that investigation.

I'd imagine that if you wanted to create a competitor to WoWhead, you'd need something like the DbcReader at the very least, because that's how you crawl the data.

What are the legal ramifications?  I don't know.  The DBC file format is pretty self-explanatory (there are only 5 fields in the header of the original file!), so it's not like it was mind-blowingly astonishing to reverse-engineer it.  The .db2 file format is mostly the same with a few additional fields.  I am not a lawyer; the fact that Blizzard allows sites like WoWhead to do what they do seems like it would be not the worst thing ever.  But use the power responsibly; don't hack, and don't steal stuff.
