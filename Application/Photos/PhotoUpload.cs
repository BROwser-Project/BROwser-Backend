﻿using Application.Core;
using Application.Interfaces;
using Database;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Photos
{
    /// <summary>
    /// Upload the photo to the Cloudinary storage
    /// </summary>
    public class PhotoUpload
    {
        /// <summary>
        /// File seletion to upload
        /// </summary>
        public class Command : IRequest<Result<Photo>>
        {
            public IFormFile File { get; set; }
        }

        /// <summary>
        /// Photo upload - business operation handler
        /// </summary>
        public class Handler : IRequestHandler<Command, Result<Photo>>
        {
            private readonly DataContext _context;
            private readonly IPhotoAccessor _photoAccessor;
            private readonly IUserAccessor _userAccessor;

            public Handler(DataContext context, IPhotoAccessor photoAccessor, IUserAccessor userAccessor)
            {
                _userAccessor = userAccessor;
                _photoAccessor = photoAccessor;
                _context = context;
            }

            // Upload the photo
            public async Task<Result<Photo>> Handle(Command request, CancellationToken cancellationToken)
            {
                // Current logged in user by email
                var user = await _context.Users.Include(p => p.Photos).FirstOrDefaultAsync(x => x.Email == _userAccessor.GetEmail());
                if (user == null) return null;

                var photoUploadResult = await _photoAccessor.AddPhoto(request.File);

                var photo = new Photo
                {
                    Url = photoUploadResult.Url,
                    Id = photoUploadResult.PublicId
                };

                if (!user.Photos.Any(x => x.IsMain)) photo.IsMain = true;

                user.Photos.Add(photo);

                var result = await _context.SaveChangesAsync() > 0;

                if (result) return Result<Photo>.Success(photo);
                return Result<Photo>.Failure("Problem adding photo");
            }
        }
    }
}
