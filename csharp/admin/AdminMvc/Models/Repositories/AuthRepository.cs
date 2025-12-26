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
using Health.Direct.Config.Client.AuthManagerService;

namespace Health.Direct.Admin.Console.Models.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AuthManagerClient m_client;

        public AuthRepository(AuthManagerClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            m_client = client;
        }

        protected AuthManagerClient Client { get { return m_client; } }

        public bool IsEnabled(string username)
        {
            if (username == null)
            {
                throw new ArgumentNullException(nameof(username));
            }

            var user = GetUser(username);
            return user != null && user.Status == EntityStatus.Enabled;
        }

        public bool ValidateUser(string username, string password)
        {
            if (username == null)
            {
                throw new ArgumentNullException(nameof(username));
            }

            var user = GetUser(username);
            if (user == null)
            {
                return false;
            }

            // Uses your partial PasswordHash (Administrator, string)
            var hash = new PasswordHash(user, password);

            var response = m_client.ValidateUser(new ValidateUserRequest(user.Username, hash));
            return response != null && response.ValidateUserResult;
        }

        private Administrator GetUser(string username)
        {
            var response = m_client.GetUser(new GetUserRequest(username));
            return response != null ? response.GetUserResult : null;
        }
    }
}