﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PhotosDataBase.Model;

namespace PhotosDataBase.Migrations
{
    [DbContext(typeof(PhotosDbContext))]
    [Migration("20190130111507_addedProperty_CameraModel")]
    partial class addedProperty_CameraModel
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.1-servicing-10028")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("PhotosDataBase.Model.PhotoFileInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("AddToBaseDate");

                    b.Property<string>("CameraModel");

                    b.Property<DateTime>("CreateDate");

                    b.Property<string>("FileName");

                    b.Property<string>("FileNameFull");

                    b.Property<int>("FileSize");

                    b.Property<int>("Height");

                    b.Property<byte[]>("PhotoPreview");

                    b.Property<int>("Width");

                    b.HasKey("Id");

                    b.ToTable("PhotoFiles");
                });
#pragma warning restore 612, 618
        }
    }
}