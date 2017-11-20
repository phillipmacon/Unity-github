using System;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    [Serializable]
    class UserSettingsView : Subview
    {
        private static readonly Vector2 viewSize = new Vector2(325, 125);
        private const string WindowTitle = "User Settings";

        private const string GitConfigTitle = "Git Configuration";
        private const string GitConfigNameLabel = "Name";
        private const string GitConfigEmailLabel = "Email";
        private const string GitConfigUserSave = "Save User";

        [NonSerialized] private bool isBusy;
        [NonSerialized] private bool userDataHasChanged;

        [SerializeField] private string gitName;
        [SerializeField] private string gitEmail;
        [SerializeField] private string newGitName;
        [SerializeField] private string newGitEmail;
        [SerializeField] private bool needsSaving;

        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            Title = WindowTitle;
            Size = viewSize;
        }

        public override void OnDataUpdate()
        {
            base.OnDataUpdate();
            MaybeUpdateData();
        }

        public override void OnGUI()
        {
            GUILayout.Label(GitConfigTitle, EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(IsBusy || Parent.IsBusy);
            {
                EditorGUI.BeginChangeCheck();
                {
                    newGitName = EditorGUILayout.TextField(GitConfigNameLabel, newGitName);
                    newGitEmail = EditorGUILayout.TextField(GitConfigEmailLabel, newGitEmail);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    needsSaving = !(string.IsNullOrEmpty(newGitName) || string.IsNullOrEmpty(newGitEmail))
                        && (newGitName != gitName || newGitEmail != gitEmail);
                }

                EditorGUI.BeginDisabledGroup(!needsSaving);
                {
                    if (GUILayout.Button(GitConfigUserSave, GUILayout.ExpandWidth(false)))
                    {
                        GUI.FocusControl(null);
                        isBusy = true;

                        GitClient.SetConfigUserAndEmail(newGitName, newGitEmail)
                                 .FinallyInUI((success, exception, user) => {
                                     isBusy = false;
                                     if (success)
                                     {
                                         if (Repository != null)
                                         {
                                             Repository.User.Name = gitName = newGitName;
                                             Repository.User.Email = gitEmail = newGitEmail;
                                         }
                                         else
                                         {
                                             gitName = newGitName;
                                             gitEmail = newGitEmail;
                                         }

                                         needsSaving = false;

                                         Redraw();
                                         Finish(true);
                                     }
                                 })
                                 .Start();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.EndDisabledGroup();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            userDataHasChanged = true;
        }

        private void MaybeUpdateData()
        {
            if (userDataHasChanged)
            {
                userDataHasChanged = false;

                if (Repository == null)
                {
                    UpdateUserDataFromClient();
                }
                else
                {
                    newGitName = gitName = Repository.User.Name;
                    newGitEmail = gitEmail = Repository.User.Email;
                    needsSaving = false;
                }
            }
        }

        private void UpdateUserDataFromClient()
        {
            if (String.IsNullOrEmpty(EntryPoint.Environment.GitExecutablePath))
            {
                return;
            }

            if (GitClient == null)
            {
                return;
            }

            Logger.Trace("Update user data from GitClient");

            GitClient.GetConfigUserAndEmail()
                .ThenInUI((success, user) => {
                    if (success && !String.IsNullOrEmpty(user.Name) && !String.IsNullOrEmpty(user.Email))
                    {
                        newGitName = gitName = user.Name;
                        newGitEmail = gitEmail = user.Email;
                        needsSaving = false;
                        Redraw();
                    }
                }).Start();
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }
    }
}