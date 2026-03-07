/*
 Copyright (c) 2010-2025, Direct Project
 All rights reserved.

 Authors:
    Joe Shook       Joseph.Shook@Surescripts.com

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of The Direct Project (directproject.org) nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/

using System;
using System.Security.Cryptography;
using System.Text;

namespace Health.Direct.Config.Client.AuthManagerService
{
    // Extends generated PasswordHash (adds constructors + client-side hashing).
    public partial class PasswordHash
    {
        public PasswordHash() { }

        public PasswordHash(string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
                throw new ArgumentNullException(nameof(hashedPassword));
            this.HashedPassword = hashedPassword;
        }

        public PasswordHash(Administrator user, string password)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (password == null) throw new ArgumentNullException(nameof(password));
            this.HashedPassword = ComputeHash(user, password);
        }

        static string ComputeHash(Administrator user, string password)
        {
            // Must match the server-side algorithm in config.store\PasswordHash.cs
            var source = user.Username.ToLower() + "|" + user.CreateDate.ToString("yyyyMMdd'T'HHmmss") + "|" + password;
            return Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(source)));
        }
    }
}