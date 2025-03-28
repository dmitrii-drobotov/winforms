﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Forms.BinaryFormat;

/// <summary>
///  Class information with type info and the source library.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/847b0b6a-86af-4203-8ed0-f84345f845b9">
///    [MS-NRBF] 2.3.2.1
///   </see>
///  </para>
/// </remarks>
internal sealed class ClassWithMembersAndTypes : ClassRecord, IRecord<ClassWithMembersAndTypes>
{
    public MemberTypeInfo MemberTypeInfo { get; }
    public Id LibraryId { get; }

    public ClassWithMembersAndTypes(
        ClassInfo classInfo,
        Id libraryId,
        MemberTypeInfo memberTypeInfo,
        IReadOnlyList<object> memberValues)
        : base(classInfo, memberValues)
    {
        MemberTypeInfo = memberTypeInfo;
        LibraryId = libraryId;
    }

    public ClassWithMembersAndTypes(
        ClassInfo classInfo,
        Id libraryId,
        MemberTypeInfo memberTypeInfo,
        params object[] memberValues)
        : this(classInfo, libraryId, memberTypeInfo, (IReadOnlyList<object>)memberValues)
    {
    }

    public static RecordType RecordType => RecordType.ClassWithMembersAndTypes;

    static ClassWithMembersAndTypes IBinaryFormatParseable<ClassWithMembersAndTypes>.Parse(
        BinaryReader reader,
        RecordMap recordMap)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader, out Count memberCount);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, memberCount);

        ClassWithMembersAndTypes record = new(
            classInfo,
            reader.ReadInt32(),
            memberTypeInfo,
            ReadValuesFromMemberTypeInfo(reader, recordMap, memberTypeInfo));

        // Index this record by the id of the embedded ClassInfo's object id.
        recordMap[record.ClassInfo.ObjectId] = record;
        return record;
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((byte)RecordType);
        ClassInfo.Write(writer);
        MemberTypeInfo.Write(writer);
        writer.Write(LibraryId);
        WriteValuesFromMemberTypeInfo(writer, MemberTypeInfo, MemberValues);
    }
}
