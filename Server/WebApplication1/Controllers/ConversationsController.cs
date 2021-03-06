﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApplication1.Data;
using WebApplication1.Entities;
using WebApplication1.Helpers;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private IConversationService _conversationService;
        private IConversationUserService _conversationUserService;
        private IMessageService _messageService;
        private IUserService _userService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;

        public ConversationsController(
              IConversationService conversationService,
              IConversationUserService conversationUserService,
              IMessageService messageService,
              IUserService userService,
              IMapper mapper,
              IOptions<AppSettings> appSettings)
        {
            _conversationService = conversationService;
            _conversationUserService = conversationUserService;
            _messageService = messageService;
            _userService = userService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        // GET: api/Conversations
        [Authorize(Roles = Role.Admin)]
        [HttpGet]
        public IActionResult GetConversations()
        {
            try
            {
                var conversations = _conversationService.GetAll();
                var model = _mapper.Map<IList<ConversationViewModel>>(conversations);
                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/Conversations/5
        [HttpGet("{id}")]
        public IActionResult GetConversation(int id)
        {
            try
            {
                var conversation = _conversationService.GetById(id);
                var model = _mapper.Map<ConversationViewModel>(conversation);
                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/Conversations/5
        [HttpPut("{id}")]
        public IActionResult PutConversation(int id, [FromBody]ConversationUpdateModel model)
        {
            try
            {
                var conversation = _mapper.Map<Conversation>(model);
                conversation.Id = id;

                // Update
                _conversationService.Update(conversation);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/Conversations
        [HttpPost]
        public IActionResult PostConversation([FromBody]ConversationCreationModel model)
        {
            try
            {
                // Map model to entity
                var conversation = _mapper.Map<Conversation>(model);

                var currentUserId = Auth.GetUserIdFromClaims(this);
                conversation.HostUserId = currentUserId;

                // Create
                var createdConversation = _conversationService.Create(conversation);

                // Add current user as member
                _conversationUserService.Create(new ConversationUser()
                {
                    ConversationId = createdConversation.Id,
                    UserId = currentUserId
                });

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/Conversations/5
        [HttpDelete("{id}")]
        public IActionResult DeleteConversation(int id)
        {
            try
            {
                // Get conversation
                var conversation = _conversationService.GetById(id);

                // Check if current user is admin or conversation host
                var currentUserId = Auth.GetUserIdFromClaims(this);
                var currentUser = _userService.GetById(currentUserId);
                if (currentUser.Role != Role.Admin && currentUserId != conversation.HostUserId)
                    throw new Exception("Have no right");

                // Delete messages
                var messageIds = _conversationService.GetMessages(id).Select(x => x.Id).ToList();
                foreach (var messageId in messageIds)
                {
                    _messageService.Delete(messageId);
                }

                // Delete members
                var members = _conversationService.GetConversationUsers(id).ToList();
                foreach (var member in members)
                {
                    _conversationUserService.Delete(member);
                }

                // Delete conversation
                _conversationService.Delete(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Members")]
        public IActionResult PostMember([FromBody]ConversationUserModel model)
        {
            try
            {
                // Map model to entity
                var member = _mapper.Map<ConversationUser>(model);

                // Create
                _conversationUserService.Create(member);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("Members/{id}/{userId}")]
        public IActionResult DeleteMember(int id, int userId)
        {
            try
            {
                // Map model to entity
                var member = _mapper.Map<ConversationUser>(new ConversationUserModel()
                {
                    ConversationId = id,
                    UserId = userId
                });

                var conversation = _conversationService.GetById(id);
                var currentUserId = Auth.GetUserIdFromClaims(this);
                if (currentUserId != conversation.HostUserId && currentUserId != userId)
                    throw new Exception("Have no right");

                // Delete messages
                var messageIds = _conversationService.GetMessages(id).Where(x => x.SenderId == userId).Select(x => x.Id).ToList();
                foreach (var messageId in messageIds)
                {
                    _messageService.Delete(messageId);
                }
                    
                // Delete member
                _conversationUserService.Delete(member);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Members")]
        public IActionResult GetMembers(int id)
        {
            try
            {
                var conversationUsers = _conversationService.GetConversationUsers(id);
                var users = conversationUsers.Select(x => x.User);
                var model = _mapper.Map<IList<UserViewModel>>(users);
                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Messages")]
        public IActionResult GetMessages(int id)
        {
            try
            {
                var messages = _conversationService.GetMessages(id);
                var model = _mapper.Map<IList<MessageViewModel>>(messages);
                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Messages/New")]
        public IActionResult GetNewMessages(int id, long lastTimeSpan)
        {
            try
            {
                var timeMessages = _conversationService.GetNewMessages(id, lastTimeSpan);
                var time = timeMessages.Item1;
                var messages = timeMessages.Item2;
                var mappedMessages = _mapper.Map<IList<MessageViewModel>>(messages);
                return Ok(new Tuple<long, IList<MessageViewModel>>(time, mappedMessages));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Messages/Last")]
        public IActionResult GetLastMessage(int id)
        {
            try
            {
                var lastMessage = _conversationService.GetLastMessage(id);
                var messageView = _mapper.Map<MessageViewModel>(lastMessage);
                return Ok(messageView);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("WithMembers")]
        public IActionResult PostConversationWithMembers([FromBody]ConversationCreationWithMemberModel model)
        {
            try
            {
                var members = new List<User>();

                // Add current member
                var currentUserId = Auth.GetUserIdFromClaims(this);
                model.UserIds.Add(currentUserId);

                if (model.UserIds.Count <= 1)
                    throw new Exception("Too few members");

                foreach (var userId in model.UserIds)
                {
                    if (model.UserIds.Where(x => x == userId).Count() > 1)
                        throw new Exception("Duplicated members");

                    members.Add(_userService.GetById(userId));
                }

                // Check
                var foundConversation = _conversationService.GetByMembers(members);
                if (foundConversation != null)
                {
                    return Conflict(_mapper.Map<ConversationViewModel>(foundConversation));
                }
            
                // Map model to entity
                var conversation = new Conversation()
                {
                    Name = model.Name,
                    HostUserId = currentUserId
                };

                // Create
                var createdConversation = _conversationService.Create(conversation);

                foreach (var user in members)
                {
                    _conversationUserService.Create(new ConversationUser()
                    {
                        ConversationId = createdConversation.Id,
                        UserId = user.Id
                    });
                }

                var conversationView = _mapper.Map<ConversationViewModel>(createdConversation);
                return Ok(conversationView);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/Conversations/5
        [HttpPut("HostMember/{id}")]
        public IActionResult PutConversationHostMember(int id, [FromBody]ConversationHostUpdateModel model)
        {
            try
            {
                // Get current host
                var hostUserId = _conversationService.GetById(id).HostUserId;
                
                // Check if current user is host
                var currentUserId = Auth.GetUserIdFromClaims(this);

                if (hostUserId != currentUserId)
                    throw new Exception("Have no right");                

                // Update
                _conversationService.UpdateHostMember(id, model.UserId);

                var updatedConversation = _conversationService.GetById(id);
                var updatedConversationView = _mapper.Map<ConversationViewModel>(updatedConversation);
                return Ok(updatedConversationView);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Seen/{id}")]
        public IActionResult GetConversationUserSeens(int id)
        {
            try
            {
                var members = _conversationUserService.GetAll().Where(x => x.ConversationId == id).ToList();
                var seenView = _mapper.Map<IList<ConversationSeenViewModel>>(members);
                return Ok(seenView);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("Seen/{id}")]
        public IActionResult PutConversationUserSeen(int id, [FromBody]ConversationSeenUpdateModel model)
        {
            try
            {
                // Check valid conversation
                var conversation = _conversationService.GetById(id);

                // Check valid message
                var message = conversation.Messages.FirstOrDefault(x => x.Id == model.MessageId);
                if (message == null)
                    throw new Exception("Conversation not contains the message");

                // Check valid member
                var currentUserId = Auth.GetUserIdFromClaims(this);
                var member = _conversationService.GetConversationUsers(id).FirstOrDefault(x => x.UserId == currentUserId);
                if (member == null)
                    throw new Exception("User is not member of the conversation");

                // Update
                _conversationUserService.UpdateSeen(member, message);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
