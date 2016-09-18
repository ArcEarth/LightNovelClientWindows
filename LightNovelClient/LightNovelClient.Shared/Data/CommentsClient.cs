using System;
using System.Collections.Generic;
using System.Text;
using FireSharp.Interfaces;
using FireSharp;
using FireSharp.Config;
using System.Threading.Tasks;

namespace LightNovel.Data
{
    //interface ICommentsClient
    //{
    //}

    struct UserData
    {
        public ulong uid;
        public string user_name;
        public string email;
        public string password;
    };

    class CommentsClient
    {
        FirebaseConfig  config;
        IFirebaseClient client;
        UserData user;

        public CommentsClient(int uid, string auth_token)
        {
            user.user_name = "admin";
            user.uid = 1;
            config = new FirebaseConfig
            {
                AuthSecret = auth_token,
                BasePath = "https://novelcommentshost.firebaseio.com/"
            };
            client = new FirebaseClient(config);
        }

        public async Task<IDictionary<string, IDictionary<string, Comment>>> GetCommentsAsync(string novel_path)
        {
            var response = await client.GetAsync("comments/" + novel_path);
            var result = response.ResultAs<IDictionary<string, IDictionary<string, Comment>>>();
            return result;
        }

        public async Task<bool> CreateCommentAsync(string novel_path, string lineUid, string content)
        {
            if (user.uid == 0 
                || String.IsNullOrEmpty(novel_path) 
                || String.IsNullOrEmpty(lineUid)) return false;
            Comment cmt = new Comment { cn=content, u=user.user_name };
            var response = await client.PushAsync("comments/" + novel_path+'/'+ lineUid, cmt);
            return response.Result.name != null;
        }

    }
}
