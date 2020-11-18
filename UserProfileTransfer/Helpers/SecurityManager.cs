using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserProfileTransfer.Models;

namespace UserProfileTransfer.Helpers
{
    class SecurityManager
    {
        private const string SysAdminRoleName = "System Administrator";
        private readonly CrmServiceClient _client;
        private List<Entity> _teams;
        private List<Entity> _roles;
        private List<Entity> _businessUnits;
        private List<Entity> _users;
        private List<Entity> _queues;

        public SecurityManager(CrmServiceClient client)
        {
            _client = client;
            Initialise();
        }

        private void Initialise()
        {
            this._teams = GetTeams();
            this._roles = GetSecurityRoles();
            this._businessUnits = GetBusinessUnits();
            this._users = GetUsers();
            this._queues = GetQueues();
        }
       

        public string ExportUserSecurity()
        {
            var jsonPayLoad = "";
            try
            {
                var userProfiles = new UserSecurityProfileList();

                foreach (var user in this._users)
                {

                    var disabled = user.GetAttributeValue<bool>("isdisabled");

                    if (!disabled)
                    {
                        //build object
                        var userProfile = new UserSecurityProfile() { emails = new List<string>() { user.GetAttributeValue<string>("domainname") } };

                        userProfile.businessUnit = user.GetAttributeValue<EntityReference>("businessunitid").Name;

                        var userRoles = GetUserSecurityRoles(user.Id);
                        userProfile.roles = userRoles.Select(entity => entity.GetAttributeValue<string>("name")).ToList();
                        userProfile.roles.Sort();

                        //need to exclude from list
                        var userTeams = GetUserTeams(user.Id);
                        userProfile.teams = userTeams.Select(entity => entity.GetAttributeValue<string>("name")).ToList();
                        userProfile.teams.Sort();

                        //need to exclude from list - do.where.select
                        var userQueues = GetUserQueues(user.Id);
                        userProfile.queues = userQueues
                            .Where(item => item.GetAttributeValue<AliasedValue>("team.name") == null &&
                                item.GetAttributeValue<EntityReference>("ownerid").Id != user.Id)
                            .Select(entity => entity.GetAttributeValue<string>("name")).ToList();
                        userProfile.queues.Sort();

                        if (userProfiles.Where<UserSecurityProfile>(profile =>
                            user.GetAttributeValue<EntityReference>("businessunitid").Name == profile.businessUnit
                            && userProfile.roles.SequenceEqual(profile.roles)
                            && userProfile.teams.SequenceEqual(profile.teams)
                            && userProfile.queues.SequenceEqual(profile.queues))
                        .FirstOrDefault() != null)
                        {
                            userProfiles.Where<UserSecurityProfile>(profile =>
                            user.GetAttributeValue<EntityReference>("businessunitid").Name == profile.businessUnit
                             && userProfile.roles.SequenceEqual(profile.roles)
                             && userProfile.teams.SequenceEqual(profile.teams)
                             && userProfile.queues.SequenceEqual(profile.queues))
                            .FirstOrDefault().emails.Add(userProfile.emails[0]);
                        }
                        else
                        {
                            userProfiles.Add(userProfile);
                        }
                    }
                }

                jsonPayLoad = JsonConvert.SerializeObject(userProfiles);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failure to process Team security roles: {e.Message}");
            }
            return jsonPayLoad;
        }

        public void ConfigureUser(UserSecurityProfile userModel)
        {
            foreach (var payloadUserEmail in userModel.emails)
            {
                var currentUser = this._users.FirstOrDefault(entity => entity.GetAttributeValue<string>("domainname").ToLower() == payloadUserEmail.ToLower());
                if (currentUser != null)
                {
                    var disabled = currentUser.GetAttributeValue<bool>("isdisabled");

                    if (!disabled)
                    {
                        //business unit
                        //business units are unique
                        var userBusinessUnitId = currentUser.GetAttributeValue<EntityReference>("businessunitid").Id;
                        var userBusinessUnitName = this._businessUnits.FirstOrDefault(entity => entity.Id == userBusinessUnitId)
                            ?.GetAttributeValue<string>("name");


                        var moveToBusinessUnit = this._businessUnits.FirstOrDefault(bu =>
                            bu.GetAttributeValue<string>("name") == userModel.businessUnit);

                        //# assign user to business unit
                        if (moveToBusinessUnit != null && moveToBusinessUnit.Id != userBusinessUnitId)
                        {
                            SetCrmUserBusinessUnit(currentUser.Id, moveToBusinessUnit.Id);
                            currentUser["businessunitid"] = moveToBusinessUnit.Id;
                            userBusinessUnitId = moveToBusinessUnit.Id;
                        }
                        else if (moveToBusinessUnit == null)
                        {
                            // Could not confirm user's expected business unit.
                        }

                        var userRoles = GetUserSecurityRoles(currentUser.Id);

                        //roles
                        //security roles have unique names
                        var rolesToRemove = userRoles.Where(item =>
                            userModel.roles.Contains(item.GetAttributeValue<string>("name")) == false).Distinct();

                        foreach (var role in rolesToRemove)
                        {
                            RemoveSecurityRoleFromUser(currentUser, role.Id);
                        }

                        var rolesToAdd = userModel.roles.Where(item =>
                            userRoles.FirstOrDefault(entity => item == entity.GetAttributeValue<string>("name")) == null).Distinct();

                        foreach (var role in rolesToAdd)
                        {
                            var roleId = this._roles.Where(item =>
                            item.GetAttributeValue<string>("name") == role && item.GetAttributeValue<EntityReference>("businessunitid").Id == userBusinessUnitId).Distinct();
                            AddSecurityRoleToUser(currentUser, roleId.First<Entity>().Id);
                        }

                        var userTeams = GetUserTeams(currentUser.Id);

                        //teams
                        var teamsToRemove = userTeams.Where(item =>
                            userModel.teams.Contains(item.GetAttributeValue<string>("name")) == false).Distinct();

                        foreach (var team in teamsToRemove)
                        {
                            RemoveUserFromTeam(currentUser, team.Id);
                        }

                        var teamsToAdd = userModel.teams.Where(item =>
                            userTeams.FirstOrDefault(entity => item == entity.GetAttributeValue<string>("name")) == null).Distinct();

                        foreach (var team in teamsToAdd)
                        {
                            var teamId = this._teams.Where(item =>
                            item.GetAttributeValue<string>("name") == team).Distinct();
                            AddUserToTeam(currentUser, teamId.First<Entity>().Id);
                        }

                        //need to get queues, but not include any where they are the owner (personal queues)
                        //also need to exclude queues that are attached to a default business unit 

                        var userQueues = GetUserQueues(currentUser.Id);

                        var queuesToAdd = userModel.queues.Where(item =>
                            userQueues.FirstOrDefault(entity => item == entity.GetAttributeValue<string>("name")) == null).Distinct();

                        foreach (var queue in queuesToAdd)
                        {
                            var queueId = this._queues.Where(item =>
                            item.GetAttributeValue<string>("name") == queue).Distinct();
                            AddUserToQueue(currentUser, queueId.First<Entity>().Id);
                        }

                        var queuesToRemove = userQueues.Where(item =>
                            userModel.queues.Contains(item.GetAttributeValue<string>("name")) == false &&
                            item.GetAttributeValue<AliasedValue>("team.name") == null &&
                            item.GetAttributeValue<EntityReference>("ownerid").Id != currentUser.Id
                            ).Distinct();

                        foreach (var queue in queuesToRemove)
                        {
                            RemoveUserFromQueue(currentUser, queue.Id);
                        }
                    }
                    else
                    {
                        //can we still remove all security roles etc in case they get reactivated?
                    }
                }
                else
                {
                    //couldnt find user
                }
            }
        }
        
        private void AddUserToQueue(Entity user, Guid queueId)
        {
            var addPrincipalToQueueRequest = new AddPrincipalToQueueRequest
            {
                Principal = user,
                QueueId = queueId
            };

            _client.Execute(addPrincipalToQueueRequest);
        }

        private void RemoveUserFromQueue(Entity user, Guid queueId)
        {
            var queueReference = new EntityReference("queue", queueId);
            var queueRefs = new EntityReferenceCollection(new List<EntityReference>() { queueReference });
            _client.Disassociate(
                "systemuser",
                user.Id,
                new Relationship("queuemembership_association"),
                queueRefs);
        }

        private void AddUserToTeam(Entity user, Guid teamId)
        {
            var addUserRequest = new AddMembersTeamRequest();
            addUserRequest.MemberIds = new[] { user.Id };
            addUserRequest.TeamId = teamId;

            _client.Execute(addUserRequest);
        }

        private void RemoveUserFromTeam(Entity user, Guid teamId)
        {
            var removeUserRequest = new RemoveMembersTeamRequest();
            removeUserRequest.MemberIds = new[] { user.Id };
            removeUserRequest.TeamId = teamId;

            _client.Execute(removeUserRequest);
        }

        private void RemoveSecurityRoleFromUser(Entity user, Guid roleId)
        {
            var roleReference = new EntityReference("role", roleId);
            var roleRefs = new EntityReferenceCollection(new List<EntityReference>() { roleReference });
            _client.Disassociate(
                "systemuser",
                user.Id,
                new Relationship("systemuserroles_association"),
                roleRefs);
        }



        private void AddSecurityRoleToUser(Entity user, Guid roleId)
        {
            var roleReference = new EntityReference("role", roleId);
            var roleRefs = new EntityReferenceCollection(new List<EntityReference>() { roleReference });
            _client.Associate(
                "systemuser",
                user.Id,
                new Relationship("systemuserroles_association"),
                roleRefs);
        }

        private List<Entity> GetUserSecurityRoles(Guid userId)
        {
            var fetch = $@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' no-lock='true'>
	              <entity name='role'>
	                <attribute name='name' />
	                <attribute name='roleid' />
	                <order attribute='name' descending='false' />
	                <link-entity name='systemuserroles' from='roleid' to='roleid' visible='false' intersect='true'>
	                  <link-entity name='systemuser' from='systemuserid' to='systemuserid' alias='user'>
                      <attribute name='businessunitid'/>
	                    <filter type='and'>
	                      <condition attribute='systemuserid' operator='eq' value='{userId}' />
	                    </filter>
	                  </link-entity>
	                </link-entity>
	              </entity>
	            </fetch>";

            var query = new FetchExpression(fetch);
            var response = _client.RetrieveMultiple(query);

            return response.Entities.ToList();
        }

        private List<Entity> GetUserTeams(Guid userId)
        {
            var fetch = $@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' no-lock='true'>
	              <entity name='team'>
	                <attribute name='name' />
	                <attribute name='teamid' />    
                    <filter>
                        <condition attribute='isdefault' operator='eq' value='0' />
                    </filter>
                    <order attribute='name' descending='false' />
	                <link-entity name='teammembership' from='teamid' to='teamid' visible='false' intersect='true'>
	                  <link-entity name='systemuser' from='systemuserid' to='systemuserid' alias='user'>
	                    <filter type='and'>
	                      <condition attribute='systemuserid' operator='eq' value='{userId}' />
	                    </filter>
	                  </link-entity>
	                </link-entity>
	              </entity>
	            </fetch>";

            var query = new FetchExpression(fetch);
            var response = _client.RetrieveMultiple(query);

            return response.Entities.ToList();
        }

        private List<Entity> GetUserQueues(Guid userId)
        {
            var fetch = $@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' no-lock='true'>
	              <entity name='queue'>
	                <attribute name='name' />
	                <attribute name='queueid' />    
	                <attribute name='ownerid' />    
                    <order attribute='name' descending='false' />
	                <link-entity name='queuemembership' from='queueid' to='queueid' visible='false' intersect='true'>
	                  <link-entity name='systemuser' from='systemuserid' to='systemuserid' alias='user'>
	                    <filter type='and'>
	                      <condition attribute='systemuserid' operator='eq' value='{userId}' />
	                    </filter>
	                  </link-entity>
	                </link-entity>
                    <link-entity name='team' from='queueid' to='queueid' link-type='outer' alias='team' >
                        <attribute name='name' />
                        <attribute name='isdefault' />
                    </link-entity>
	              </entity>
	            </fetch>";

            var query = new FetchExpression(fetch);
            var response = _client.RetrieveMultiple(query);

            return response.Entities.ToList();
        }

        private void SetCrmUserBusinessUnit(Guid userId, Guid businessUnitId)
        {
            var request = new SetBusinessSystemUserRequest()
            {
                ReassignPrincipal = new EntityReference("systemuser", userId),
                UserId = userId,
                BusinessId = businessUnitId
            };

            _client.Execute(request);
        }

        public List<Entity> GetBusinessUnits()
        {
            var query = new QueryExpression("businessunit")
            {
                ColumnSet = new ColumnSet("businessunitid", "name")
            };

            var response = this._client.RetrieveMultiple(query);

            return response.Entities.ToList();
        }

        public List<Entity> GetTeams()
        {
            var query = new QueryExpression("team")
            {
                ColumnSet = new ColumnSet("teamid", "name")
            };

            var response = _client.RetrieveMultiple(query);
            return response.Entities.ToList();
        }

        public List<Entity> GetSecurityRoles()
        {
            var query = new QueryExpression("role")
            {
                ColumnSet = new ColumnSet("roleid", "name", "businessunitid")
            };

            var response = _client.RetrieveMultiple(query);
            return response.Entities.ToList();
        }

        public List<Entity> GetUsers()
        {
            var query = new QueryExpression("systemuser")
            {
                ColumnSet = new ColumnSet("systemuserid", "fullname", "domainname", "businessunitid", "isdisabled")
            };

            var response = _client.RetrieveMultiple(query);

            return response.Entities.ToList();
        }
        public List<Entity> GetQueues()
        {
            var query = new QueryExpression("queue")
            {
                ColumnSet = new ColumnSet("queueid", "name")
            };

            var response = _client.RetrieveMultiple(query);

            return response.Entities.ToList();
        }
    }
}
