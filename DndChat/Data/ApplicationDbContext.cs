using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using DndChat.Models;

namespace DndChat.Data
{
    public class ApplicationDbContext : IdentityDbContext<ChatUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) {}
        
        public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
        public DbSet<ChatMembership> ChatMemberships => Set<ChatMembership>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ChatRoom: ensure join codes are unique and reasonably sized.
            modelBuilder.Entity<ChatRoom>(entity =>
            {
                entity.HasIndex(room => room.JoinCode).IsUnique();
                entity.Property(room => room.JoinCode).HasMaxLength(64);
            });

            // ChatMembership: prevent duplicate memberships per (room, user) and set relationship to ChatRoom.
            modelBuilder.Entity<ChatMembership>(entity =>
            {
                entity.HasIndex(membership => new { membership.ChatRoomId, membership.UserId }).IsUnique();

                entity.HasOne(membership => membership.ChatRoom)
                    .WithMany(room => room.Members)
                    .HasForeignKey(membership => membership.ChatRoomId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ChatMessage: link each message to its room and cap message length.
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasOne(message => message.ChatRoom)
                    .WithMany(room => room.Messages)
                    .HasForeignKey(message => message.ChatRoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(message => message.MessageText).HasMaxLength(2000);
            });
        }

    }
}
