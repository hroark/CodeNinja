from django.contrib import admin
from django.urls import include, path

urlpatterns = [
    path("posts/", include("Posts.urls")),
    path("admin/", admin.site.urls),
]