﻿using System;
using System.Diagnostics;
using System.IO;
//using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Synology.Api.Client.ApiDescription;
using Synology.Api.Client.Apis.FileStation.Upload.Models;
using Synology.Api.Client.Session;

namespace Synology.Api.Client.Apis.FileStation.Upload
{
    public class FileStationUploadEndpoint : IFileStationUploadEndpoint
    {
        private readonly ISynologyHttpClient _synologyHttpClient;
        private readonly IApiInfo _apiInfo;
        private readonly ISynologySession _session;


        public FileStationUploadEndpoint(ISynologyHttpClient synologyHttpClient,
                                         IApiInfo apiInfo,
                                         ISynologySession session)
        {
            _synologyHttpClient = synologyHttpClient;
            _apiInfo = apiInfo;
            _session = session;
        }

        /// <inheritdoc />
        public Task<FileStationUploadResponse> UploadAsync(string filePath, string destination, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var filename =Path.GetFileName(filePath);

            var bytes = File.ReadAllBytes(filePath);
            var memoryStream = new MemoryStream(bytes);

            var fileContent = GetFileContent(memoryStream, filename);

            return SendRequest(fileContent, destination, overwrite);
        }

        /// <inheritdoc />
        public Task<FileStationUploadResponse> UploadAsync(byte[] bytes, string filename, string destination, bool overwrite)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentNullException(nameof(filename));
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var memoryStream = new MemoryStream(bytes);
            var fileContent = GetFileContent(memoryStream, filename);

            return SendRequest(fileContent, destination, overwrite);
        }

        private Task<FileStationUploadResponse> SendRequest(StreamContent fileContent, string destination, bool overwrite)
        {
            var boundary = Guid.NewGuid().ToString();

            var formData = new MultipartFormDataContent(boundary);
            {
                // The request will fail if there are quotes around the boundary value
                formData.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
                formData.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", boundary));

                var overwriteValue = overwrite ? "true" : "false";
                if (_apiInfo.Version >= 3)
                {
                    overwriteValue = overwrite ? "overwrite" : "skip";
                }

                formData.Add(GetStringContent("api", _apiInfo.Name));
                formData.Add(GetStringContent("version", _apiInfo.Version.ToString()));
                formData.Add(GetStringContent("method", "upload"));
                formData.Add(GetStringContent("path", destination));
                formData.Add(GetStringContent("overwrite", overwriteValue));
                formData.Add(GetStringContent("create_parents", "true"));

                //prevent ObjectDisposedException
                //await fileContent.LoadIntoBufferAsync();
                formData.Add(fileContent);

                return _synologyHttpClient.PostAsync<FileStationUploadResponse>(_apiInfo, "upload", formData, _session);
            }
        }

        private StringContent GetStringContent(string name, string value)
        {
            var sc = new StringContent(value);
            sc.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = $"\"{name}\""
            };

            //the API does not like the "Content-Type" header
            sc.Headers.ContentType = null;

            return sc;
        }

        private StreamContent GetFileContent(Stream stream, string filename)
        {
            var fileContent = new StreamContent(stream);
            
            // this is required to send non ascii characters in the filename
            var urlEncodedFilename = Uri.EscapeDataString(filename);
            var headerValue = $@"form-data; name=""file""; filename=""{filename}""; filename*=UTF-8''{urlEncodedFilename}";
            var bytes = Encoding.UTF8.GetBytes(headerValue);

            // ���� ��θ� �����մϴ�. �� ��δ� ���ϴ� ��� ������ �� �ֽ��ϴ�.
            string filePath = @"D:\project_albaam\output.txt";
            using var fileStream = new FileStream(filePath, FileMode.Create);
            using var streamWriter = new StreamWriter(fileStream);
            headerValue = bytes.Aggregate("", (current, b) => {
                var b156 = '\u009c';
                var b128 = '\u0080';
                var b154 = '\u009a';
                var sum = current + (char)b;
                if (b == 128)
                {
                    sum = current + b128;
                    //Encoding.Unicode.GetString()
                }
                else if (b == 154)
                {
                    sum = current + b154;
                }
                else if (b == 156)
                {
                    sum = current + b156;
                }
                UnityEngine.Debug.Log(b);
                UnityEngine.Debug.Log((char)b);
                UnityEngine.Debug.Log(current);
                UnityEngine.Debug.Log(sum);

                Console.SetOut(streamWriter);

                Console.WriteLine(b);
                Console.WriteLine((char)b);
                Console.WriteLine(current);
                Console.WriteLine(sum);
                return sum;
            });

            // �ʿ��ϴٸ�, �ܼ� ����� �ٽ� �⺻ ������� �ǵ��� �� �ֽ��ϴ�.
            StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
            Console.WriteLine("����Ϸ�");
            fileContent.Headers.Add("Content-Disposition", headerValue);

            return fileContent;
        }
    }
}
